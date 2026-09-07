using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Data;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：在洞室外重连被厚壁截开的天然支洞，保证避开洞室侧壁的全洞穴连通性。
    public static class AntChamberOuterRoutes
    {
        //函数职责：逐次接回主洞尚未连通的开放区域，直到洞穴掩码中的所有地面均可抵达主洞。
        public static void Reconnect(Map map, DesertPitLayoutData data)
        {
            HashSet<IntVec3> forbidden = new HashSet<IntVec3>();
            foreach (AntChamberLayout room in data.AntChambers)
                forbidden.UnionWith(room.Footprint);
            HashSet<IntVec3> connected = AntChamberConnector.CollectMainCave(map, data.MainCenter);
            while (true)
            {
                bool missing = false;
                foreach (IntVec3 cell in map.AllCells)
                    if (MapGenerator.Caves[cell] > 0f && !connected.Contains(cell)) { missing = true; break; }
                if (!missing) return;
                List<IntVec3> path = ConnectNearestRegion(map, data, connected, forbidden);
                connected = AntChamberConnector.CollectMainCave(map, data.MainCenter);
                if (!connected.Contains(path[path.Count - 1]))
                    throw new InvalidOperationException("蚁巢外围连接没有接通目标洞段，停止继续重连。");
            }
        }

        //函数职责：从主洞边界按岩性寻找可接通的外侧洞段，曲线可绕过洞体凹口但不能切穿实际岩层。
        private static List<IntVec3> ConnectNearestRegion(Map map, DesertPitLayoutData data, HashSet<IntVec3> connected, HashSet<IntVec3> forbidden)
        {
            List<IntVec3> frontier = new List<IntVec3>();
            //只把会向未连通区域扩张的边界放入优先队列，跳过大片主洞内部地面。
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!connected.Contains(cell) || forbidden.Contains(cell)) continue;
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (next.InBounds(map) && !connected.Contains(next) && !forbidden.Contains(next))
                    { frontier.Add(cell); break; }
                }
            }
            Func<IntVec3, bool> canOpen = cell => cell.InBounds(map) && !forbidden.Contains(cell);
            List<IntVec3> path = AntTunnelPathfinder.Find(map, frontier, canOpen,
                cell => MapGenerator.Caves[cell] > 0f && !connected.Contains(cell));
            AntTunnelCarver.Carve(map, data, path, canOpen);
            return path;
        }
    }
}
