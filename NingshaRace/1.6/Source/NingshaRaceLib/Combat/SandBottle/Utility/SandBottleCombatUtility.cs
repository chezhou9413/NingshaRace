using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.SandBottle.Verbs;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Utility;

namespace NingshaRaceLib.Combat.SandBottle.Utility
{
    //类职责：计算沙瓶扇形范围、结算热能伤害与滞缓，并播放直接挂载的抛沙粒子。
    public static class SandBottleCombatUtility
    {
        //函数职责：计算瞄准时显示的扇形格子，使预览与实际视线阻挡范围一致。
        public static List<IntVec3> FindConeCells(Pawn attacker, IntVec3 targetCell, float coneAngle, float range)
        {
            List<IntVec3> cells = new List<IntVec3>();
            if (attacker?.Map == null || !targetCell.InBounds(attacker.Map))
            {
                return cells;
            }

            IntVec3 origin = attacker.Position;
            Vector3 originPosition = origin.ToVector3Shifted();
            Vector3 attackDirection = HorizontalDirection(originPosition, targetCell.ToVector3Shifted());
            float halfAngle = coneAngle * 0.5f;
            float rangeSquared = range * range;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, range, useCenter: false))
            {
                if (!cell.InBounds(attacker.Map))
                {
                    continue;
                }

                Vector3 offset = cell.ToVector3Shifted() - originPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude > rangeSquared || Vector3.Angle(attackDirection, offset) > halfAngle)
                {
                    continue;
                }

                if (GenSight.LineOfSight(origin, cell, attacker.Map))
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        //函数职责：判断目标是否为攻击者同地图上的存活 Pawn 或建筑，不限制所属阵营。
        public static bool IsValidDamageTarget(Pawn attacker, Thing target)
        {
            if (attacker == null
                || target == null
                || target == attacker
                || !target.Spawned
                || target.Map != attacker.Map)
            {
                return false;
            }

            if (target is Pawn pawn)
            {
                return !pawn.Dead;
            }

            return target.def.category == ThingCategory.Building;
        }

        //函数职责：扫描固定方向扇形中的唯一 Pawn 与建筑，并确保每个目标都与攻击起点保持视线连通。
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
            Vector3 originPosition = origin.ToVector3Shifted();
            float halfAngle = coneAngle * 0.5f;
            float rangeSquared = range * range;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, range, useCenter: false))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                Vector3 offset = cell.ToVector3Shifted() - originPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude > rangeSquared
                    || Vector3.Angle(attackDirection, offset) > halfAngle
                    || !GenSight.LineOfSight(origin, cell, map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing target = things[i];
                    if (addedTargets.Contains(target) || !IsValidDamageTarget(attacker, target))
                    {
                        continue;
                    }

                    addedTargets.Add(target);
                    targets.Add(target);
                }
            }

            targets.Sort((left, right) =>
                left.Position.DistanceToSquared(origin).CompareTo(right.Position.DistanceToSquared(origin)));
            return targets;
        }

        //函数职责：对攻击瞬间扇形内的全部 Pawn 与建筑各结算一次热能伤害，并给存活 Pawn 刷新滞缓时间。
        public static void ApplyAttack(Verb_SandBottle verb, IntVec3 origin, Vector3 attackDirection)
        {
            Pawn attacker = verb.CasterPawn;
            Map map = attacker.Map;
            List<Thing> targets = FindTargets(
                attacker,
                map,
                origin,
                attackDirection,
                verb.Props.coneAngle,
                verb.EffectiveRange);

            for (int i = 0; i < targets.Count; i++)
            {
                Thing target = targets[i];
                ApplyDamage(verb, target, attackDirection);
                if (target is Pawn pawn && !pawn.Dead)
                {
                    ApplyOrRefreshSlow(pawn, verb.Props.slowDurationTicks);
                }
            }
        }

        //函数职责：从 ChezhouLib 普通预制体表直接创建抛沙特效并朝攻击方向播放。
        public static void SpawnEffect(Verb_SandBottle verb, Vector3 attackDirection)
        {
            VerbProperties_SandBottle props = verb.Props;
            string effectKey = props.effectModId + "_" + props.effectName;
            Vector3 spawnPosition = verb.CasterPawn.DrawPos;
            spawnPosition.y = AltitudeLayer.MoteOverheadLow.AltitudeFor() + 0.02f;
            //Prefab 发射轴统一为本地正 X，根节点绕 X 轴九十度贴合地图，再用 Yaw 朝向目标。
            float directionYaw = Vector3.SignedAngle(Vector3.right, attackDirection, Vector3.up);
            Quaternion rimWorldRotation = Quaternion.Euler(90f, directionYaw, 0f);
            Vector3 effectScale = new Vector3(
                props.effectScale,
                props.effectScale,
                props.effectScale * props.effectDepthScale);
            DirectPrefabEffectUtility.Spawn(
                effectKey,
                spawnPosition,
                rimWorldRotation,
                effectScale,
                props.effectLifetime);
        }

        //函数职责：按 XML 配置创建带来源与方向的沙瓶伤害。
        private static void ApplyDamage(Verb_SandBottle verb, Thing target, Vector3 attackDirection)
        {
            Pawn attacker = verb.CasterPawn;
            ThingWithComps weapon = verb.EquipmentSource;
            DamageInfo damageInfo = new DamageInfo(
                verb.Props.damageDef,
                verb.Props.damageAmount,
                0f,
                -1f,
                attacker,
                null,
                weapon?.def ?? DefOfRefs.NingshaRace_SandBottle,
                DamageInfo.SourceCategory.ThingOrUnknown,
                target);
            damageInfo.SetAngle(attackDirection);
            target.TakeDamage(damageInfo);
        }

        //函数职责：给 Pawn 添加唯一沙尘滞缓状态，已有状态只重置剩余时间而不叠加倍率。
        private static void ApplyOrRefreshSlow(Pawn pawn, int durationTicks)
        {
            Hediff slow = pawn.health.hediffSet.GetFirstHediffOfDef(DefOfRefs.NingshaRace_SandBottleSlow);
            if (slow == null)
            {
                slow = pawn.health.AddHediff(DefOfRefs.NingshaRace_SandBottleSlow);
            }

            HediffComp_Disappears disappears = slow.TryGetComp<HediffComp_Disappears>();
            if (disappears == null)
            {
                Log.Error("[NingshaRace] NingshaRace_SandBottleSlow 缺少 HediffComp_Disappears。");
                return;
            }

            disappears.SetDuration(durationTicks);
        }

        //函数职责：把任意地图水平向量归一化，并避免同格目标产生无效旋转。
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
    }
}
