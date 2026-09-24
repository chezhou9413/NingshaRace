using System;
using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Topology
{
    //类职责：从已连通的洞地选择具有通道净空的接入点，保持受保护洞室的实际岩壁完整。
    internal static class DesertPitPassageEndpoints
    {
        //函数职责：沿目标所在的四邻接洞地寻找最近的可雕刻接入点，不穿岩壁或连接孤立空腔。
        public static IntVec3 FindTarget(Map map, IntVec3 target, HashSet<IntVec3> forbidden)
        {
            if (!target.InBounds(map) || MapGenerator.Caves[target] <= 0f)
                throw new InvalidOperationException("地下通道目标不在已有洞地内，目标：" + target);
            HashSet<IntVec3> visited = new HashSet<IntVec3> { target };
            Queue<IntVec3> queue = new Queue<IntVec3>();
            queue.Enqueue(target);
            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                if (DesertPitRoomBrush.CanPass(map, cell, forbidden)) return cell;
                //这里只沿已开放地面寻找接入点；实际雕刻仍排除整个蚁巢及其他保留区域。
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (next.InBounds(map) && MapGenerator.Caves[next] > 0f && visited.Add(next))
                        queue.Enqueue(next);
                }
            }
            throw new InvalidOperationException("地下通道目标洞地没有满足宽度的接入点，目标：" + target
                + "，已连通洞地格数：" + visited.Count);
        }
    }
}
