using System.Collections.Generic;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Steps;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Landmarks
{
    //类职责：提供沙漠巨坑局部地貌装饰物的候选池收集、加权抽取和间距判断。
    public static class DesertPitLandmarkPlacementUtility
    {
        //函数职责：为一个局部地貌中心预收集可放置装饰物的候选格，避免每次放置都重复扫描半径范围。
        public static List<IntVec3> CollectLocalCandidates(Map map, DesertPitLayoutData data, IntVec3 center, float radius)
        {
            List<IntVec3> candidates = new List<IntVec3>();
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (DesertPitLandmarkUtility.CanPlaceLandmarkThing(map, data, candidate))
                {
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        //函数职责：从预收集候选池中按权重抽取一个符合间距的格子并移出候选池。
        public static bool TryTakeLocalCell(Map map, IntVec3 center, float radius, List<IntVec3> candidates, List<IntVec3> placed, ThingDef def, out IntVec3 cell)
        {
            float totalWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (SpacingAllows(placed, candidates[i], def))
                {
                    totalWeight += LocalCellWeight(map, center, radius, candidates[i]);
                }
            }

            if (totalWeight <= 0f)
            {
                cell = IntVec3.Invalid;
                return false;
            }

            float roll = Rand.Value * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 candidate = candidates[i];
                if (!SpacingAllows(placed, candidate, def))
                {
                    continue;
                }

                roll -= LocalCellWeight(map, center, radius, candidate);
                if (roll <= 0f)
                {
                    cell = candidate;
                    candidates.RemoveAt(i);
                    return true;
                }
            }

            cell = IntVec3.Invalid;
            return false;
        }

        //函数职责：计算局部区域内单格权重，使地貌边缘贴近洞壁但中心保留主体密度。
        private static float LocalCellWeight(Map map, IntVec3 center, float radius, IntVec3 cell)
        {
            float distance = cell.DistanceTo(center) / radius;
            float weight = Mathf.Lerp(5f, 1f, distance);
            if (DesertPitGenUtility.NearCaveEdge(map, cell, 4))
            {
                weight += 4f;
            }

            if (cell.GetTerrain(map).defName == "Sandstone_Rough")
            {
                weight += 2f;
            }

            return Mathf.Max(weight, 0.2f);
        }

        //函数职责：判断地貌装饰物之间是否保留符合其尺寸的最小距离。
        private static bool SpacingAllows(List<IntVec3> placed, IntVec3 cell, ThingDef def)
        {
            float minDistance = 0.85f;
            if (def.defName == "NingshaRace_DesertPitCeilingSandfall")
            {
                minDistance = 3.5f;
            }
            else if (DesertPitDecorationUtility.IsCrystal(def))
            {
                minDistance = def.defName == "NingshaRace_DesertPitGlowCrystalShard" ? 1.35f : 1.8f;
            }
            else if (DesertPitDecorationUtility.IsLargeDecoration(def))
            {
                minDistance = 1.15f;
            }

            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].DistanceTo(cell) < minDistance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
