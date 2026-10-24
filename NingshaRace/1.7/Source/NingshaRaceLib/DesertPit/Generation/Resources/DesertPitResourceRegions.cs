using System;
using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Resources
{
    //类职责：将真实资源区域按通道行进方向或洞壁周向划成面积均衡的局部分区。
    internal static class DesertPitResourceRegions
    {
        //函数职责：按最近中心线节点排列通道格，使各份配额沿通道分散。
        public static List<List<IntVec3>> AlongPassage(IEnumerable<IntVec3> area, List<IntVec3> centerline)
        {
            List<IntVec3> cells = new List<IntVec3>(area);
            Dictionary<IntVec3, int> order = new Dictionary<IntVec3, int>();
            foreach (IntVec3 cell in cells)
            {
                int nearest = 0;
                float distance = float.MaxValue;
                for (int i = 0; i < centerline.Count; i++)
                {
                    float next = cell.DistanceToSquared(centerline[i]);
                    if (next < distance) { distance = next; nearest = i; }
                }
                order.Add(cell, nearest);
            }
            cells.Sort((a, b) => Compare(order[a].CompareTo(order[b]), a, b));
            return Split(cells);
        }

        //函数职责：沿洞室中心的周向排列真实洞壁带，避免把全部晶体集中到同一侧。
        public static List<List<IntVec3>> AlongRoom(IEnumerable<IntVec3> area, IntVec3 center)
        {
            List<IntVec3> cells = new List<IntVec3>(area);
            cells.Sort((a, b) => Compare(Math.Atan2(a.z - center.z, a.x - center.x)
                .CompareTo(Math.Atan2(b.z - center.z, b.x - center.x)), a, b));
            return Split(cells);
        }

        //函数职责：对主排序相同的格子使用坐标稳定排序，避免集合遍历顺序影响分区。
        private static int Compare(int primary, IntVec3 a, IntVec3 b)
        {
            if (primary != 0) return primary;
            int x = a.x.CompareTo(b.x);
            return x != 0 ? x : a.z.CompareTo(b.z);
        }

        //函数职责：按向上取整的百格份数均分面积，避免末尾一两格单独承担一整份资源。
        private static List<List<IntVec3>> Split(List<IntVec3> cells)
        {
            List<List<IntVec3>> result = new List<List<IntVec3>>();
            int count = (cells.Count + 99) / 100;
            int offset = 0;
            for (int i = 0; i < count; i++)
            {
                int size = cells.Count / count + (i < cells.Count % count ? 1 : 0);
                result.Add(cells.GetRange(offset, size));
                offset += size;
            }
            return result;
        }
    }
}
