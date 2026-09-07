using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Generation.Chambers;
using NingshaRaceLib.DesertPit.Generation.Data;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Topology
{
    //类职责：用偏心椭圆与缓变岩性生成自然洞室，并以宽洞口连接连续洞群。
    internal static class DesertPitRoomBrush
    {
        //函数职责：构造实际地面集合，只保留与中心相通的部分，不修改地图实体。
        public static DesertPitRoom Create(Map map, IntVec3 center, float rx, float rz, float rotation, HashSet<IntVec3> forbidden)
            => Create(map, center, new DesertPitCaveProfile(rx, rz, rotation), forbidden);

        //函数职责：把可复用的洞壁形状栅格化为完整洞室，裁去保护岩层并保留中心连通部分。
        public static DesertPitRoom Create(Map map, IntVec3 center, DesertPitCaveProfile profile, HashSet<IntVec3> forbidden)
        {
            DesertPitRoom room = new DesertPitRoom { Center = center, RadiusX = profile.RadiusX,
                RadiusZ = profile.RadiusZ, Rotation = profile.Rotation };
            foreach (IntVec3 offset in profile.FloorOffsets)
            {
                IntVec3 cell = center + offset;
                if (CanOpen(map, cell, forbidden) && cell.DistanceToEdge(map) >= 3) room.Floor.Add(cell);
            }
            //噪声与保留岩层可能切下孤立边角，不把这些边角留成不可进入的小空腔。
            KeepConnected(room);
            return room;
        }

        //函数职责：把完整洞室写入洞穴掩码，并记录后续内容生成与验收锚点。
        public static void Apply(DesertPitLayoutData data, DesertPitRoom room, bool smallRoom)
        {
            foreach (IntVec3 cell in room.Floor) MapGenerator.Caves[cell] = 1f;
            data.Rooms.Add(room);
            if (smallRoom) data.SmallRooms.Add(room.Center);
        }

        //函数职责：用宽而短的洞口接通相邻洞室，受蚁巢阻挡时先求完整可行路线再雕刻。
        public static void Connect(Map map, DesertPitLayoutData data, IntVec3 from, IntVec3 to,
            float width, HashSet<IntVec3> forbidden)
        {
            List<IntVec3> path = BuildCurvedPath(from, to);
            Func<IntVec3, bool> canPass = center => CanOpen(map, center, forbidden)
                && CanOpen(map, center + IntVec3.North, forbidden) && CanOpen(map, center + IntVec3.South, forbidden)
                && CanOpen(map, center + IntVec3.East, forbidden) && CanOpen(map, center + IntVec3.West, forbidden);
            bool obstructed = false;
            foreach (IntVec3 cell in path)
                if (!canPass(cell)) { obstructed = true; break; }
            if (obstructed)
                path = AntTunnelPathfinder.Find(map, new[] { from }, canPass, cell => cell == to);
            float phase = Rand.Range(0f, 6.28f);
            for (int i = 0; i < path.Count; i++)
            {
                IntVec3 center = path[i];
                float radius = Mathf.Max(2f, width * (0.78f + 0.22f * Mathf.Sin(i * 0.13f + phase)));
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!CanOpen(map, cell, forbidden)) continue;
                    MapGenerator.Caves[cell] = 1f;
                    //仅保护步行核心，两旁宽洞口仍允许生成正常洞穴内容。
                    if (cell.DistanceToSquared(center) <= 2.25f) data.ProtectedRouteCells.Add(cell);
                }
            }
        }

        //函数职责：沿平滑偏转的中心线串联宽窄洞口，按四邻接补齐采样间隙而不是切出直线走廊。
        private static List<IntVec3> BuildCurvedPath(IntVec3 from, IntVec3 to)
        {
            Vector3 delta = to.ToVector3() - from.ToVector3();
            Vector3 side = new Vector3(-delta.z, 0f, delta.x).normalized;
            float bend = Mathf.Min(9f, delta.magnitude * 0.18f) * (Rand.Bool ? 1f : -1f);
            int samples = Mathf.Max(2, Mathf.CeilToInt(delta.magnitude));
            List<IntVec3> result = new List<IntVec3> { from };
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                IntVec3 next = i == samples ? to : (from.ToVector3Shifted() + delta * t
                    + side * (Mathf.Sin(t * Mathf.PI) * bend)).ToIntVec3();
                foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(result[result.Count - 1], next))
                    if (cell != result[result.Count - 1]) result.Add(cell);
            }
            return result;
        }

        //函数职责：收集不可被天然洞群和水道再次雕刻的完整蚁巢洞室占地。
        public static HashSet<IntVec3> AntFootprints(DesertPitLayoutData data)
        {
            HashSet<IntVec3> result = new HashSet<IntVec3>();
            foreach (AntChamberLayout room in data.AntChambers) result.UnionWith(room.Footprint);
            return result;
        }

        //函数职责：判断普通洞室笔刷是否允许使用地图中的指定格子。
        private static bool CanOpen(Map map, IntVec3 cell, HashSet<IntVec3> forbidden)
            => cell.InBounds(map) && (forbidden == null || !forbidden.Contains(cell));

        //函数职责：移除被岩脊隔开的轮廓边角，确保实际洞室全部四邻接连通。
        private static void KeepConnected(DesertPitRoom room)
        {
            HashSet<IntVec3> reached = new HashSet<IntVec3>();
            Queue<IntVec3> queue = new Queue<IntVec3>();
            if (room.Floor.Contains(room.Center)) { reached.Add(room.Center); queue.Enqueue(room.Center); }
            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (room.Floor.Contains(next) && reached.Add(next)) queue.Enqueue(next);
                }
            }
            room.Floor.IntersectWith(reached);
        }
    }
}
