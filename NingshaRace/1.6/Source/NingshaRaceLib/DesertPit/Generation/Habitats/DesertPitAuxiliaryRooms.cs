using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Generation.Chambers;
using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Topology;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：围绕蚁巢及主次洞室规划附属小洞室、窄通道和带水潭的坠砂生态洞室。
    internal static class DesertPitAuxiliaryRooms
    {
        //函数职责：先补充蚁巢外围结构，再按全图总数安排主次洞室周边生态区。
        public static void Generate(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            HashSet<IntVec3> walls = DesertPitRoomBrush.AntFootprints(data);
            HashSet<IntVec3> forbidden = new HashSet<IntVec3>(walls);
            forbidden.UnionWith(data.ReservedSceneCells);
            foreach (AntChamberLayout nest in data.AntChambers)
            {
                int count = settings.antSideRoomCount.RandomInRange;
                for (int i = 0; i < count; i++)
                {
                    float radius = settings.antSideRoomRadius.RandomInRange;
                    float distance = Mathf.Max(nest.Bounds.Width, nest.Bounds.Height) * 0.5f + radius + 3f;
                    DesertPitRoom room = FindAround(map, nest.Nest, distance, radius, forbidden);
                    DesertPitRoomBrush.Apply(data, room, true);
                    ConnectEdge(map, data, room, nest.ExternalAccessCell, walls);
                    AddExtraConnection(map, data, room, nest.ExternalAccessCell, walls, settings.extraTunnelChance);
                    data.AntSideRooms.Add(room);
                    //各个附属洞室之间保留岩壁，额外连接只能通过明确雕刻的窄通道形成。
                    AddRoomBuffer(room, forbidden);
                }
            }

            List<DesertPitRoom> anchors = new List<DesertPitRoom> { data.Rooms[0] };
            anchors.AddRange(data.SecondaryRooms);
            int total = settings.habitatRoomCount.RandomInRange;
            int start = Rand.Range(0, anchors.Count);
            for (int i = 0; i < total; i++)
            {
                DesertPitRoom anchor = anchors[(start + i) % anchors.Count];
                float radius = settings.habitatRoomRadius.RandomInRange;
                bool sandfall = Rand.Chance(settings.habitatSandfallChance);
                bool pond = sandfall && Rand.Chance(settings.habitatPondChance);
                DesertPitRoom room = FindAround(map, anchor.Center, Mathf.Max(anchor.RadiusX, anchor.RadiusZ) + radius, radius, forbidden,
                    pond ? settings.habitatPondRadius.min + 1f : 0f);
                DesertPitRoomBrush.Apply(data, room, true);
                ConnectEdge(map, data, room, anchor.Center, walls);
                DesertPitHabitat habitat = new DesertPitHabitat { room = room, sandfall = sandfall, pond = pond };
                habitat.floor.UnionWith(room.Floor);
                habitat.floor.ExceptWith(data.ReservedSceneCells);
                if (habitat.pond) PlanPond(data, habitat, settings);
                data.Habitats.Add(habitat);
                data.ReservedSceneCells.UnionWith(habitat.floor);
                walls.UnionWith(habitat.floor);
                AddRoomBuffer(room, forbidden);
            }
        }

        //函数职责：按环形候选寻找有独立岩壁轮廓的小洞室，空间不足时明确报告布局错误。
        private static DesertPitRoom FindAround(Map map, IntVec3 anchor, float distance, float radius, HashSet<IntVec3> forbidden, float clearRadius = 0f)
        {
            float phase = Rand.Range(0f, 360f);
            for (int i = 0; i < 8; i++)
            {
                IntVec3 desired = (anchor.ToVector3Shifted() + Vector3Utility.FromAngleFlat(phase + i * 137.508f) * distance).ToIntVec3();
                DesertPitRoom room = DesertPitRoomPlacement.Find(map, desired, radius, forbidden, radius + 7f, 0.3f);
                if (room == null) continue;
                bool centerClear = true;
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(room.Center, clearRadius, true))
                    if (!cell.InBounds(map) || cell.DistanceToEdge(map) < 3 || forbidden.Contains(cell)) { centerClear = false; break; }
                if (!centerClear) continue;
                //水潭中心保留完整净空，外围仍使用自然洞壁轮廓，避免小洞室的噪声凹口切进水潭。
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(room.Center, clearRadius, true)) room.Floor.Add(cell);
                return room;
            }
            throw new InvalidOperationException("地下附属洞室空间不足，中心：" + anchor + "，半径：" + radius);
        }

        //函数职责：从小洞室靠近目标的边缘开窄通道，保留洞室中心的水潭和内容空间。
        private static void ConnectEdge(Map map, DesertPitLayoutData data, DesertPitRoom room, IntVec3 target, HashSet<IntVec3> walls)
        {
            IntVec3 edge = room.Center;
            float best = float.MaxValue;
            foreach (IntVec3 cell in room.Floor)
            {
                bool clear = true;
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                    if (walls.Contains(cell + offset)) { clear = false; break; }
                if (!clear) continue;
                float distance = cell.DistanceToSquared(target);
                if (distance < best) { edge = cell; best = distance; }
            }
            DesertPitRoomBrush.Connect(map, data, edge, target, 1.3f, walls, 1f);
        }

        //函数职责：按概率给附属洞室增加通往其他洞室的第二条窄通道。
        private static void AddExtraConnection(Map map, DesertPitLayoutData data, DesertPitRoom room,
            IntVec3 primary, HashSet<IntVec3> walls, float chance)
        {
            if (!Rand.Chance(chance)) return;
            DesertPitRoom target = null;
            float best = float.MaxValue;
            foreach (DesertPitRoom other in data.Rooms)
            {
                if (other == room || other.Center.DistanceTo(primary) < 5f) continue;
                float score = other.Center.DistanceToSquared(room.Center);
                if (score < best) { target = other; best = score; }
            }
            if (target != null) ConnectEdge(map, data, room, target.Center, walls);
        }

        //函数职责：保留小洞室地面和一圈岩壁，避免后续小洞室直接吞并现有轮廓。
        private static void AddRoomBuffer(DesertPitRoom room, HashSet<IntVec3> forbidden)
        {
            foreach (IntVec3 cell in room.Floor)
            {
                forbidden.Add(cell);
                foreach (IntVec3 offset in GenAdj.AdjacentCells) forbidden.Add(cell + offset);
            }
        }

        //函数职责：根据实际洞壁限制中央水潭半径，保留泥土岸带和已有通道。
        private static void PlanPond(DesertPitLayoutData data, DesertPitHabitat habitat, DefModExtension_DesertPitLayout settings)
        {
            float clearance = Mathf.Min(habitat.room.RadiusX, habitat.room.RadiusZ);
            foreach (IntVec3 cell in habitat.floor)
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                    if (!habitat.floor.Contains(cell + offset)) clearance = Mathf.Min(clearance, (cell + offset).DistanceTo(habitat.room.Center));
            float radius = Mathf.Min(settings.habitatPondRadius.RandomInRange, clearance - 1f);
            if (radius < settings.habitatPondRadius.min)
                throw new InvalidOperationException("生态洞室无法容纳水潭与岸带，请增大生态洞室半径。");
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(habitat.room.Center, radius, true))
                if (habitat.floor.Contains(cell) && !data.ProtectedRouteCells.Contains(cell)) habitat.water.Add(cell);
        }
    }
}
