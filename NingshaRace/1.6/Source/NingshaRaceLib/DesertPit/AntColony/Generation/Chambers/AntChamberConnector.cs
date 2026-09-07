using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Data;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：从洞室唯一出口寻找自然弯曲的主洞连接线，保护实际岩层而不是矩形包围盒。
    public static class AntChamberConnector
    {
        //函数职责：只沿四邻接开放洞格标记真实连通主洞，不把孤立空腔当作出口目标。
        public static HashSet<IntVec3> CollectMainCave(Map map, IntVec3 center)
        {
            HashSet<IntVec3> visited = new HashSet<IntVec3> { center };
            Queue<IntVec3> queue = new Queue<IntVec3>();
            queue.Enqueue(center);
            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (next.InBounds(map) && MapGenerator.Caves[next] > 0f && visited.Add(next)) queue.Enqueue(next);
                }
            }
            return visited;
        }

        //函数职责：在洞口方向保持完整通行宽度，使用岩性加权连接线和圆形笔刷接入主洞。
        public static void Connect(Map map, DesertPitLayoutData data, AntChamberLayout room, HashSet<IntVec3> mainCave)
        {
            Func<IntVec3, bool> canOpen = cell => CanOpen(map, data, room, cell);
            List<IntVec3> path = AntTunnelPathfinder.Find(map, new[] { room.Mouth },
                cell => CanCarve(cell, canOpen),
                cell => !room.Footprint.Contains(cell) && !data.ReservedSceneCells.Contains(cell) && mainCave.Contains(cell));
            AntTunnelCarver.Carve(map, data, path, canOpen);
        }

        //函数职责：保证通道宽度不越界、不穿过其他场景，也不在自身厚壁上开出第二道口。
        private static bool CanCarve(IntVec3 center, Func<IntVec3, bool> canOpen)
        {
            foreach (IntVec3 offset in GenAdj.AdjacentCellsAndInside)
                if (!canOpen(center + offset)) return false;
            return true;
        }

        //函数职责：只允许通过现有洞口截面进出本洞室，拒绝切入侧壁、弯道岩脊或其他预留场景。
        private static bool CanOpen(Map map, DesertPitLayoutData data, AntChamberLayout room, IntVec3 cell)
        {
            if (!cell.InBounds(map) || cell.x < 2 || cell.z < 2 || cell.x >= map.Size.x - 2 || cell.z >= map.Size.z - 2) return false;
            if (!room.Footprint.Contains(cell)) return !data.ReservedSceneCells.Contains(cell);
            IntVec3 forward = AntChamberLayout.Rotate(new IntVec3(1, 0, 0), room.Direction);
            IntVec3 delta = cell - room.Mouth;
            return delta.x * forward.x + delta.z * forward.z >= -2 && room.PassageCells.Contains(cell);
        }
    }
}
