using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.AltarMissions.Map
{
    //类职责：从救援地图的真实空地选择侵蚀体位置，保证入口安全距离和待救者方向的连通性。
    internal static class AltarRescuePlacementUtility
    {
        //函数职责：在地图生成线程一次收集可站立、无人物且距玩家入口至少二十格的候选位置。
        public static List<IntVec3> CollectCandidates(Verse.Map map)
        {
            List<IntVec3> candidates = new List<IntVec3>();
            IntVec3 playerStart = MapGenerator.PlayerStartSpot;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.Standable(map) && cell.GetFirstPawn(map) == null
                    && (!playerStart.IsValid || cell.DistanceTo(playerStart) >= 20f))
                {
                    candidates.Add(cell);
                }
            }
            return candidates;
        }

        //函数职责：按四角方向优先取出可走到待救者的空地，并从候选列表移除以避免重复占位。
        public static IntVec3 TakeCornerCell(Verse.Map map, List<IntVec3> candidates, IntVec3 corner, IntVec3 rescueCell)
        {
            candidates.Sort((left, right) => left.DistanceToSquared(corner).CompareTo(right.DistanceToSquared(corner)));
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 cell = candidates[i];
                //寻路会访问并维护区域缓存，因此与后续实体生成保持在同一地图生成线程。
                if (map.reachability.CanReach(cell, rescueCell, PathEndMode.Touch, TraverseMode.PassDoors, Danger.Deadly))
                {
                    candidates.RemoveAt(i);
                    return cell;
                }
            }
            throw new InvalidOperationException("解救任务没有与待救者连通且距玩家入场区二十格以上的侵蚀体位置。");
        }
    }
}
