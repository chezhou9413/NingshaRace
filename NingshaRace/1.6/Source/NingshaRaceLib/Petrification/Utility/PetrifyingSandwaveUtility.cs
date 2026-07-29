using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Abilities.Components;
using NingshaRaceLib.Petrification.Health;
using NingshaRaceLib.Petrification.Patches;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.Petrification.Utility
{
    //类职责：负责石化砂潮的视线安全扫描、距离严重度换算和直接挂载特效播放。
    public static class PetrifyingSandwaveUtility
    {
        //函数职责：判断 Pawn 是否为施法者同地图上的其他存活血肉目标，不限制阵营关系。
        public static bool IsValidTarget(Pawn caster, Pawn target)
        {
            return caster != null
                && target != null
                && target != caster
                && target.Spawned
                && !target.Dead
                && target.Map == caster.Map
                && target.RaceProps.IsFlesh;
        }

        //函数职责：根据距离返回本次应累计的石化严重度。
        public static float SeverityForDistance(
            float distance,
            float range,
            CompProperties_AbilityPetrifyingSandwave props)
        {
            if (distance <= props.fullPetrificationRadius)
            {
                return DefOfRefs.NingshaRace_Petrification.maxSeverity;
            }
            if (distance > range)
            {
                return 0f;
            }

            float progress = Mathf.InverseLerp(props.fullPetrificationRadius, range, distance);
            return Mathf.Lerp(props.nearSeverity, props.edgeSeverity, progress);
        }

        //函数职责：计算瞄准预览格，并按近距离满层区和外层累计区分别输出。
        public static void FindPreviewCells(
            Pawn caster,
            IntVec3 targetCell,
            float range,
            CompProperties_AbilityPetrifyingSandwave props,
            List<IntVec3> innerCells,
            List<IntVec3> outerCells)
        {
            List<IntVec3> cells = FindConeCells(caster, targetCell, range, props.coneAngle);
            for (int i = 0; i < cells.Count; i++)
            {
                IntVec3 cell = cells[i];
                float distance = Vector3.Distance(
                    caster.Position.ToVector3Shifted(),
                    cell.ToVector3Shifted());
                if (distance <= props.fullPetrificationRadius)
                {
                    innerCells.Add(cell);
                }
                else
                {
                    outerCells.Add(cell);
                }
            }
        }

        //函数职责：播放砂潮主体并对扇形内每个唯一目标累计严重度与播放命中特效。
        public static void ApplyWave(
            Pawn caster,
            IntVec3 targetCell,
            Vector3 direction,
            CompProperties_AbilityPetrifyingSandwave props)
        {
            float range = caster.abilities
                .GetAbility(DefOfRefs.NingshaRace_Ability_PetrifyingSandwave)
                .verb.EffectiveRange;
            SpawnWaveEffect(caster, direction, props);
            List<Pawn> targets = FindTargets(caster, targetCell, range, props.coneAngle);
            for (int i = 0; i < targets.Count; i++)
            {
                Pawn target = targets[i];
                float distance = Vector3.Distance(
                    caster.Position.ToVector3Shifted(),
                    target.Position.ToVector3Shifted());
                float severity = SeverityForDistance(distance, range, props);
                if (severity <= 0f)
                {
                    continue;
                }

                PetrificationUtility.AddSeverity(target, severity);
                SpawnHitEffect(target, severity, props);
            }
        }

        //函数职责：计算视线连通的扇形格子，供预览和实际目标扫描共享。
        private static List<IntVec3> FindConeCells(Pawn caster, IntVec3 targetCell, float range, float coneAngle)
        {
            List<IntVec3> cells = new List<IntVec3>();
            Map map = caster.Map;
            IntVec3 origin = caster.Position;
            Vector3 originPosition = origin.ToVector3Shifted();
            Vector3 direction = HorizontalDirection(originPosition, targetCell.ToVector3Shifted());
            float rangeSquared = range * range;
            float halfAngle = coneAngle * 0.5f;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, range, false))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                Vector3 offset = cell.ToVector3Shifted() - originPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude > rangeSquared
                    || Vector3.Angle(direction, offset) > halfAngle
                    || !GenSight.LineOfSight(origin, cell, map))
                {
                    continue;
                }

                cells.Add(cell);
            }

            return cells;
        }

        //函数职责：从实际扇形格子中收集唯一的有效血肉 Pawn，并按距离排序。
        private static List<Pawn> FindTargets(Pawn caster, IntVec3 targetCell, float range, float coneAngle)
        {
            List<Pawn> targets = new List<Pawn>();
            HashSet<Pawn> addedTargets = new HashSet<Pawn>();
            List<IntVec3> cells = FindConeCells(caster, targetCell, range, coneAngle);
            for (int i = 0; i < cells.Count; i++)
            {
                List<Thing> things = cells[i].GetThingList(caster.Map);
                for (int j = 0; j < things.Count; j++)
                {
                    Pawn pawn = things[j] as Pawn;
                    if (pawn == null || addedTargets.Contains(pawn) || !IsValidTarget(caster, pawn))
                    {
                        continue;
                    }

                    addedTargets.Add(pawn);
                    targets.Add(pawn);
                }
            }

            targets.Sort((left, right) =>
                left.Position.DistanceToSquared(caster.Position)
                    .CompareTo(right.Position.DistanceToSquared(caster.Position)));
            return targets;
        }

        //函数职责：从 ChezhouLib 普通预制体表播放朝目标方向推进的砂潮主体。
        private static void SpawnWaveEffect(
            Pawn caster,
            Vector3 direction,
            CompProperties_AbilityPetrifyingSandwave props)
        {
            Vector3 position = caster.DrawPos;
            position.y = AltitudeLayer.MoteOverheadLow.AltitudeFor() + 0.02f;
            float yaw = Vector3.SignedAngle(Vector3.right, direction, Vector3.up);
            SpawnEffect(
                props.effectModId,
                props.waveEffectName,
                position,
                Quaternion.Euler(90f, yaw, 0f),
                props.waveEffectScale,
                props.waveEffectLifetime);
        }

        //函数职责：按本次严重度增量缩放并播放包裹目标的石粉命中特效。
        private static void SpawnHitEffect(
            Pawn target,
            float severity,
            CompProperties_AbilityPetrifyingSandwave props)
        {
            Vector3 position = target.DrawPos;
            position.y = AltitudeLayer.MoteOverheadLow.AltitudeFor() + 0.025f;
            float scale = Mathf.Lerp(
                props.minHitEffectScale,
                props.maxHitEffectScale,
                Mathf.InverseLerp(props.edgeSeverity, 1f, severity));
            SpawnEffect(
                props.effectModId,
                props.hitEffectName,
                position,
                Quaternion.Euler(90f, 0f, 0f),
                scale,
                props.hitEffectLifetime);
        }

        //函数职责：按显式资源 key 直接创建一次性粒子实例并设置变换与生命周期。
        private static void SpawnEffect(
            string modId,
            string effectName,
            Vector3 position,
            Quaternion rotation,
            float scale,
            float lifetime)
        {
            string effectKey = modId + "_" + effectName;
            DirectPrefabEffectUtility.Spawn(
                effectKey,
                position,
                rotation,
                Vector3.one * scale,
                lifetime);
        }

        //函数职责：把地图平面上的任意方向归一化，并为同格输入提供稳定默认方向。
        public static Vector3 HorizontalDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
        }
    }
}
