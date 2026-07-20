using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat
{
    //类职责：计算地刺直线横排、筛选敌对目标、施加伤害并执行 PawnFlyer 击退。
    public static class GroundSpikeCombatUtility
    {
        //函数职责：沿攻击中心线建立横向三格伤害排，并单独记录每排唯一的视觉中心格。
        public static List<List<IntVec3>> BuildWaveRows(
            Map map,
            IntVec3 origin,
            IntVec3 targetCell,
            int lineHalfWidth,
            out List<IntVec3> visualCells)
        {
            List<List<IntVec3>> rows = new List<List<IntVec3>>();
            visualCells = new List<IntVec3>();
            Vector3 attackDirection = HorizontalDirection(origin.ToVector3Shifted(), targetCell.ToVector3Shifted());
            Vector3 perpendicular = new Vector3(-attackDirection.z, 0f, attackDirection.x);

            foreach (IntVec3 centerCell in GenSight.PointsOnLineOfSight(origin, targetCell))
            {
                if (centerCell == origin)
                {
                    continue;
                }

                List<IntVec3> row = new List<IntVec3>();
                for (int offset = -lineHalfWidth; offset <= lineHalfWidth; offset++)
                {
                    IntVec3 sideOffset = new IntVec3(
                        Mathf.RoundToInt(perpendicular.x * offset),
                        0,
                        Mathf.RoundToInt(perpendicular.z * offset));
                    IntVec3 cell = centerCell + sideOffset;
                    if (cell.InBounds(map))
                    {
                        row.Add(cell);
                    }
                }

                if (row.Count > 0)
                {
                    rows.Add(row);
                    visualCells.Add(centerCell);
                }
            }

            return rows;
        }

        //函数职责：返回瞄准预览需要显示的完整三格宽直线伤害格列表。
        public static List<IntVec3> FindAffectedCells(Map map, IntVec3 origin, IntVec3 targetCell, int lineHalfWidth)
        {
            List<IntVec3> cells = new List<IntVec3>();
            HashSet<IntVec3> addedCells = new HashSet<IntVec3>();
            List<List<IntVec3>> rows = BuildWaveRows(map, origin, targetCell, lineHalfWidth, out _);
            for (int i = 0; i < rows.Count; i++)
            {
                for (int cellIndex = 0; cellIndex < rows[i].Count; cellIndex++)
                {
                    if (addedCells.Add(rows[i][cellIndex]))
                    {
                        cells.Add(rows[i][cellIndex]);
                    }
                }
            }

            return cells;
        }

        //函数职责：判断地图对象是否为本次地刺攻击允许伤害的敌对目标。
        public static bool IsDamageTarget(Pawn attacker, Thing target)
        {
            if (target == null || target == attacker || target.Destroyed || !target.Spawned || !target.HostileTo(attacker))
            {
                return false;
            }

            if (target is Pawn pawn)
            {
                return !pawn.Dead;
            }

            return target.def.category == ThingCategory.Building && target.def.Fillage != FillCategory.Full;
        }

        //函数职责：使用地刺固定伤害、穿甲和来源信息对目标结算一次刺伤。
        public static DamageWorker.DamageResult ApplyDamage(Verb_GroundSpikeSummoner verb, Thing target)
        {
            Pawn attacker = verb.CasterPawn;
            ThingWithComps weapon = verb.EquipmentSource;
            VerbProperties_GroundSpikeSummoner props = verb.Props;
            DamageInfo damageInfo = new DamageInfo(
                props.damageDef,
                props.damageAmount,
                props.armorPenetration,
                -1f,
                attacker,
                null,
                weapon?.def,
                DamageInfo.SourceCategory.ThingOrUnknown,
                null,
                !attacker.Drafted);
            damageInfo.SetAngle((target.Position - attacker.Position).ToVector3());
            return target.TakeDamage(damageInfo);
        }

        //函数职责：把存活 Pawn 沿施术者到目标点的方向放入原版 PawnFlyer 并推向最远有效落点。
        public static void ApplyPawnFlyerKnockback(Pawn target, Vector3 attackDirection, int maxCells)
        {
            if (maxCells <= 0 || target == null || target.Dead || !target.Spawned || target.ParentHolder is PawnFlyer)
            {
                return;
            }

            Map map = target.Map;
            IntVec3 startCell = target.Position;
            IntVec3 step = DirectionStep(attackDirection);
            IntVec3 destination = startCell;
            for (int i = 0; i < maxCells; i++)
            {
                IntVec3 candidate = destination + step;
                if (!candidate.InBounds(map)
                    || !candidate.WalkableBy(map, target)
                    || CellContainsOtherPawn(candidate, map, target))
                {
                    break;
                }

                destination = candidate;
            }

            if (destination == startCell)
            {
                return;
            }

            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                DefOfRefs.NingshaRace_PawnFlyer_GroundSpikeKnockback,
                target,
                destination,
                null,
                null);
            GenSpawn.Spawn(flyer, startCell, map, WipeMode.Vanish);
        }

        //函数职责：把两个地图位置之间的水平向量归一化。
        public static Vector3 HorizontalDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            return direction.normalized;
        }

        //函数职责：把水平攻击向量转换为八方向地图步进。
        private static IntVec3 DirectionStep(Vector3 attackDirection)
        {
            return new IntVec3(
                Mathf.Clamp(Mathf.RoundToInt(attackDirection.x), -1, 1),
                0,
                Mathf.Clamp(Mathf.RoundToInt(attackDirection.z), -1, 1));
        }

        //函数职责：判断候选落点是否已经被其他 Pawn 占据。
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
