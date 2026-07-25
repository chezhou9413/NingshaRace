using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.SnakeBellySword.Rendering;
using NingshaRaceLib.Combat.SnakeBellySword.Tracking;
using NingshaRaceLib.Combat.SnakeBellySword.Verbs;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Utility
{
    //类职责：计算蛇腹剑扇形目标、施加分段伤害、触发动画并完成击退。
    public static class SnakeBellySwordCombatUtility
    {
        //函数职责：播放蛇腹剑攻击动画并隐藏当前装备的蛇腹剑。
        public static void BeginAttack(Verb_SnakeBellySword verb, Vector3 attackDirection)
        {
            Pawn attacker = verb.CasterPawn;
            ThingWithComps weapon = verb.EquipmentSource;
            int hiddenTicks = verb.Props.weaponHiddenTicks;
            SnakeBellySwordRenderState.HideWeapon(weapon, hiddenTicks);
            MoteMaker.MakeStaticMote(
                attacker.DrawPos,
                attacker.Map,
                DefOfRefs.NingshaRace_Mote_WhipFrameAnimation,
                verb.Props.effectScale,
                exactRot: attackDirection.ToAngleFlat());
        }

        //函数职责：计算瞄准时显示的扇形格子，使预览范围与实际攻击范围一致。
        public static List<IntVec3> FindConeCells(
            Pawn attacker,
            LocalTargetInfo primaryTarget,
            float coneAngle,
            float range)
        {
            List<IntVec3> cells = new List<IntVec3>();
            Vector3 attackDirection = HorizontalDirection(attacker.Position.ToVector3Shifted(), primaryTarget.Cell.ToVector3Shifted());
            float halfAngle = coneAngle * 0.5f;
            float maxRangeSquared = range * range;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(attacker.Position, range, useCenter: false))
            {
                if (!cell.InBounds(attacker.Map))
                {
                    continue;
                }

                Vector3 offset = cell.ToVector3Shifted() - attacker.Position.ToVector3Shifted();
                offset.y = 0f;
                if (offset.sqrMagnitude > maxRangeSquared || Vector3.Angle(attackDirection, offset) > halfAngle)
                {
                    continue;
                }

                if (GenSight.LineOfSight(attacker.Position, cell, attacker.Map))
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        //函数职责：按主目标方向筛选五格内的 Pawn 和建筑。
        public static List<Thing> FindTargets(Pawn attacker, LocalTargetInfo primaryTarget, float coneAngle, float range)
        {
            Vector3 attackDirection = HorizontalDirection(attacker.Position.ToVector3Shifted(), primaryTarget.Cell.ToVector3Shifted());
            return FindTargets(attacker, attacker.Map, attacker.Position, attackDirection, coneAngle, range);
        }

        //函数职责：按固定的挥击起点和方向筛选五格内的 Pawn 和建筑，并按距离从远到近排序。
        public static List<Thing> FindTargets(
            Pawn attacker,
            Map map,
            IntVec3 origin,
            Vector3 attackDirection,
            float coneAngle,
            float range)
        {
            List<Thing> targets = new List<Thing>();
            HashSet<Thing> addedTargets = new HashSet<Thing>();
            float halfAngle = coneAngle * 0.5f;
            float maxRangeSquared = range * range;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, range, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing target = things[i];
                    bool isPawn = target is Pawn;
                    bool isBuilding = target.def.category == ThingCategory.Building;
                    if (target == attacker || !target.Spawned || (!isPawn && !isBuilding) || addedTargets.Contains(target))
                    {
                        continue;
                    }

                    if (target is Pawn targetPawn && targetPawn.Dead)
                    {
                        continue;
                    }

                    IntVec3 targetCell = isBuilding ? cell : target.Position;
                    Vector3 offset = targetCell.ToVector3Shifted() - origin.ToVector3Shifted();
                    offset.y = 0f;
                    if (offset.sqrMagnitude > maxRangeSquared || offset.sqrMagnitude < 0.001f)
                    {
                        continue;
                    }

                    if (Vector3.Angle(attackDirection, offset) > halfAngle)
                    {
                        continue;
                    }

                    if (!GenSight.LineOfSight(origin, targetCell, map))
                    {
                        continue;
                    }

                    addedTargets.Add(target);
                    targets.Add(target);
                }
            }

            targets.Sort((left, right) =>
                right.Position.DistanceToSquared(origin).CompareTo(left.Position.DistanceToSquared(origin)));
            return targets;
        }

        //函数职责：以原版近战伤害的随机和属性倍率创建并施加一份伤害。
        public static DamageWorker.DamageResult ApplyDamage(Verb_SnakeBellySword verb, Thing target)
        {
            Pawn attacker = verb.CasterPawn;
            ThingWithComps weapon = verb.EquipmentSource;
            VerbProperties_SnakeBellySword props = verb.Props;
            float damageAmount = Rand.Range(
                props.meleeDamageBaseAmount * props.damageFactorMin,
                props.meleeDamageBaseAmount * props.damageFactorMax);
            damageAmount *= attacker.ageTracker.CurLifeStage.meleeDamageFactor;
            damageAmount *= attacker.GetStatValue(StatDefOf.MeleeDamageFactor);
            float weaponDamageMultiplier = weapon?.GetStatValue(StatDefOf.MeleeWeapon_DamageMultiplier) ?? 1f;
            damageAmount *= weaponDamageMultiplier;
            float armorPenetration = props.meleeArmorPenetrationBase;
            if (armorPenetration < 0f)
            {
                armorPenetration = damageAmount * props.automaticArmorPenetrationFactor;
            }
            else
            {
                armorPenetration *= weaponDamageMultiplier;
            }

            QualityCategory quality = QualityCategory.Normal;
            weapon?.TryGetQuality(out quality);
            DamageInfo damageInfo = new DamageInfo(
                props.meleeDamageDef,
                damageAmount,
                armorPenetration,
                -1f,
                attacker,
                null,
                weapon?.def,
                DamageInfo.SourceCategory.ThingOrUnknown,
                null,
                !attacker.Drafted,
                weaponQuality: quality);
            damageInfo.SetAngle((target.Position - attacker.Position).ToVector3());
            return target.TakeDamage(damageInfo);
        }

        //函数职责：把受伤 Pawn 沿攻击者到目标的方向推开，并在阻挡处停止。
        public static void ApplyKnockback(IntVec3 attackOrigin, Pawn target, int maxCells)
        {
            if (maxCells <= 0 || target.Dead || !target.Spawned)
            {
                return;
            }

            IntVec3 step = KnockbackStep(attackOrigin, target.Position);
            if (step == IntVec3.Zero)
            {
                return;
            }

            IntVec3 destination = target.Position;
            for (int i = 0; i < maxCells; i++)
            {
                IntVec3 candidate = destination + step;
                if (!candidate.InBounds(target.Map) || !candidate.WalkableBy(target.Map, target) || CellContainsOtherPawn(candidate, target.Map, target))
                {
                    break;
                }

                destination = candidate;
            }

            if (destination == target.Position)
            {
                return;
            }

            target.Position = destination;
            target.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
        }

        //函数职责：把任意水平攻击向量归一化，避免同格目标产生无效方向。
        public static Vector3 HorizontalDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }

        //函数职责：把目标相对位置转换为八方向击退步进。
        private static IntVec3 KnockbackStep(IntVec3 attackerPosition, IntVec3 targetPosition)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(targetPosition.x - attackerPosition.x), -1, 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(targetPosition.z - attackerPosition.z), -1, 1);
            return new IntVec3(x, 0, z);
        }

        //函数职责：判断目标格是否已经被其他 Pawn 占用。
        private static bool CellContainsOtherPawn(IntVec3 cell, Map map, Pawn target)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn pawn && pawn != target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
