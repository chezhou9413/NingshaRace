using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Landmarks
{
    //类职责：先为全部石林分配有足够地面的分散落点，避免边生成边耗尽局部候选。
    internal static class DesertPitStoneForestPlanner
    {
        //函数职责：按最远间距选取指定数量的中心，并从其他地貌候选中移除对应占地。
        public static List<IntVec3> TakeCenters(Map map, DesertPitLayoutData data, List<IntVec3> centers, int count)
        {
            List<IntVec3> candidates = centers.FindAll(cell => HasFloor(map, data, cell));
            candidates.Shuffle();
            List<IntVec3> result = new List<IntVec3>();
            while (result.Count < count)
            {
                IntVec3 best = IntVec3.Invalid;
                float bestDistance = -1f;
                foreach (IntVec3 candidate in candidates)
                {
                    float nearest = float.MaxValue;
                    foreach (IntVec3 placed in result) nearest = Mathf.Min(nearest, candidate.DistanceToSquared(placed));
                    if (nearest > bestDistance) { bestDistance = nearest; best = candidate; }
                }
                if (!best.IsValid || bestDistance < 225f)
                    throw new InvalidOperationException("石林无法满足生成数量与十五格中心间距，目标：" + count + "，可放置：" + result.Count);
                result.Add(best);
                candidates.RemoveAll(cell => cell.DistanceToSquared(best) < 225f);
            }
            foreach (IntVec3 center in result) centers.RemoveAll(cell => cell.DistanceToSquared(center) <= 225f);
            return result;
        }

        //函数职责：拒绝通道核心、湿地和只有狭窄条带地面的中心，保证石林具有实际片状占地。
        private static bool HasFloor(Map map, DesertPitLayoutData data, IntVec3 center)
        {
            if (data.ProtectedRouteCells.Contains(center) || DesertPitGenUtility.IsWaterLikeTerrain(center.GetTerrain(map))) return false;
            int free = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 4f, true))
                if (cell.InBounds(map) && DesertPitGenUtility.IsCave(map, cell) && cell.Standable(map)
                    && !data.ReservedSceneCells.Contains(cell) && !data.ProtectedRouteCells.Contains(cell)) free++;
            return free >= 25;
        }
    }
}
