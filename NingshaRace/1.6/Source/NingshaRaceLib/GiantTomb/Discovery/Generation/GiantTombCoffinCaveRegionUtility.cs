using System.Collections.Generic;
using Verse;

using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.GiantTomb.Discovery.Generation
{
    //类职责：在地图生成尚未完成初始化时，仅根据洞穴掩码计算石棺选址使用的连通区域。
    internal static class GiantTombCoffinCaveRegionUtility
    {
        //函数职责：使用独立四向队列从指定格遍历洞穴掩码，不依赖地图共享泛洪器、区域或寻路状态。
        public static HashSet<IntVec3> CollectReachable(Map map, IntVec3 root)
        {
            HashSet<IntVec3> result = new HashSet<IntVec3>();
            if (!root.InBounds(map) || !DesertPitGenUtility.IsCave(map, root))
            {
                return result;
            }

            Queue<IntVec3> open = new Queue<IntVec3>();
            result.Add(root);
            open.Enqueue(root);
            while (open.Count > 0)
            {
                IntVec3 current = open.Dequeue();
                for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
                {
                    IntVec3 adjacent = current + GenAdj.CardinalDirections[i];
                    if (adjacent.InBounds(map) && DesertPitGenUtility.IsCave(map, adjacent) && result.Add(adjacent))
                    {
                        open.Enqueue(adjacent);
                    }
                }
            }

            return result;
        }

        //函数职责：遍历全部洞穴连通区并返回最大区域，防止异常入口掩码把场景限制在单格孤岛。
        public static HashSet<IntVec3> CollectLargest(Map map)
        {
            HashSet<IntVec3> visited = new HashSet<IntVec3>();
            HashSet<IntVec3> largest = new HashSet<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (visited.Contains(cell) || !DesertPitGenUtility.IsCave(map, cell))
                {
                    continue;
                }

                HashSet<IntVec3> region = CollectReachable(map, cell);
                visited.UnionWith(region);
                if (region.Count > largest.Count)
                {
                    largest = region;
                }
            }

            return largest;
        }
    }
}
