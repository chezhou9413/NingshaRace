using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：在允许雕刻的区域内寻找受岩性缓变扰动的八向连接线，避免固定四向最短路的直角走廊。
    internal static class AntTunnelPathfinder
    {
        //函数职责：使用局部数组缓存通行与岩性代价，只读取洞穴掩码，完成寻路后返回完整路径。
        public static List<IntVec3> Find(Map map, IEnumerable<IntVec3> starts, Func<IntVec3, bool> canUse, Func<IntVec3, bool> target)
        {
            int count = map.cellIndices.NumGridCells;
            float[] costs = new float[count];
            float[] rockCosts = new float[count];
            int[] previous = new int[count];
            sbyte[] usable = new sbyte[count];
            for (int i = 0; i < count; i++) { costs[i] = float.PositiveInfinity; previous[i] = -1; }
            ModuleBase noise = new Perlin(0.085, 2.0, 0.5, 2, Rand.Int, QualityMode.Medium);
            var queue = new FastPriorityQueue<KeyValuePair<int, float>>(Comparer<KeyValuePair<int, float>>.Create(
                (a, b) => a.Value != b.Value ? a.Value.CompareTo(b.Value) : a.Key.CompareTo(b.Key)));
            foreach (IntVec3 start in starts)
            {
                if (!CanUse(map, start, usable, canUse)) continue;
                int index = map.cellIndices.CellToIndex(start);
                if (previous[index] >= 0) continue;
                previous[index] = index;
                costs[index] = 0f;
                queue.Push(new KeyValuePair<int, float>(index, 0f));
            }

            while (queue.Count > 0)
            {
                KeyValuePair<int, float> item = queue.Pop();
                if (item.Value > costs[item.Key]) continue;
                IntVec3 current = map.cellIndices.IndexToCell(item.Key);
                if (target(current)) return Reconstruct(map, previous, item.Key);
                foreach (IntVec3 offset in GenAdj.AdjacentCells)
                {
                    IntVec3 next = current + offset;
                    if (!CanUse(map, next, usable, canUse)) continue;
                    bool diagonal = offset.x != 0 && offset.z != 0;
                    //斜向连接必须能经过两侧正交格，不能从两块厚壁的对角缝中钻过去。
                    if (diagonal && (!CanUse(map, current + new IntVec3(offset.x, 0, 0), usable, canUse)
                        || !CanUse(map, current + new IntVec3(0, 0, offset.z), usable, canUse))) continue;
                    int nextIndex = map.cellIndices.CellToIndex(next);
                    if (rockCosts[nextIndex] == 0f)
                        rockCosts[nextIndex] = 1f + 2.5f * Mathf.Clamp01(0.5f + (float)noise.GetValue(next.x, 0, next.z));
                    float cost = item.Value + (diagonal ? 1.414214f : 1f) * rockCosts[nextIndex];
                    if (cost >= costs[nextIndex]) continue;
                    costs[nextIndex] = cost;
                    previous[nextIndex] = item.Key;
                    queue.Push(new KeyValuePair<int, float>(nextIndex, cost));
                }
            }
            throw new InvalidOperationException("蚁巢洞道无法在保留岩壁之外找到连通路线。");
        }

        //函数职责：缓存静态雕刻边界判定，避免在一次寻路中反复查询同一格的场景保护范围。
        private static bool CanUse(Map map, IntVec3 cell, sbyte[] usable, Func<IntVec3, bool> validator)
        {
            if (!cell.InBounds(map)) return false;
            int index = map.cellIndices.CellToIndex(cell);
            if (usable[index] == 0) usable[index] = validator(cell) ? (sbyte)1 : (sbyte)-1;
            return usable[index] == 1;
        }

        //函数职责：按前驱数组返回从连通起点到目标区域的有序路线。
        private static List<IntVec3> Reconstruct(Map map, int[] previous, int end)
        {
            List<IntVec3> path = new List<IntVec3>();
            for (int index = end; ; index = previous[index])
            {
                path.Add(map.cellIndices.IndexToCell(index));
                if (previous[index] == index) break;
            }
            path.Reverse();
            return path;
        }
    }
}
