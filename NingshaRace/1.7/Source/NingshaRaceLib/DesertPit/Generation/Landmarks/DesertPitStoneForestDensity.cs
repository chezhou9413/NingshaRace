using System;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Landmarks
{
    //类职责：按石林扩大前后的有效干地面积计算柱石配额，保持单片密度。
    internal static class DesertPitStoneForestDensity
    {
        //函数职责：用同一组地形和保护规则对比当前半径及小一点五格的参考半径。
        public static int Target(Map map, DesertPitLayoutData data, IntVec3 center, float radius)
        {
            int expanded = 0;
            int baseline = 0;
            float oldRadius = radius - 1.5f;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell)
                    || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map))
                    || cell.GetTerrain(map).passability != Traversability.Standable
                    || data.ReservedSceneCells.Contains(cell) || data.ProtectedRouteCells.Contains(cell)
                    || cell.DistanceTo(data.MainCenter) < 11f) continue;
                //仅按地形计面积，已有柱石不会降低后续石林的目标密度。
                expanded++;
                if (cell.DistanceToSquared(center) <= oldRadius * oldRadius) baseline++;
            }
            if (baseline == 0)
                throw new InvalidOperationException("石林" + center + "缺少参考干地，扩大面积：" + expanded + "，参考面积：0。");
            return Mathf.RoundToInt(Rand.RangeInclusive(42, 62) * (expanded / (float)baseline));
        }
    }
}
