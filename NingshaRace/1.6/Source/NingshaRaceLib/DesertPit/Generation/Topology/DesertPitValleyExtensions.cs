using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Hydrology;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Topology
{
    //类职责：让次要洞群沿斜河继续向外层伸展，并从两岸长出有独立空间的探索支洞。
    internal static class DesertPitValleyExtensions
    {
        //函数职责：生成一端的外围洞群和偏离河谷的支洞，返回因空间不足未放置的可选数量。
        public static int Generate(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings,
            bool positive, DesertPitRoom secondary, List<DesertPitRoom> links, HashSet<IntVec3> forbidden)
        {
            float scale = settings.Scale(map);
            Vector3 side = DesertPitRiverPlanner.Side(data);
            int missing = 0;
            int count = settings.outerRoomCount.RandomInRange;
            IntVec3 previous = secondary.Center;
            float bankSign = Rand.Bool ? 1f : -1f;
            for (int i = 0; i < count; i++)
            {
                float layer = Mathf.Lerp(settings.outerBoundary + 0.05f, 0.9f, (i + 0.3f) / count);
                IntVec3 river = DesertPitSecondaryRooms.FindRiverPoint(map, data, layer, positive);
                float radius = settings.outerRoomRadius.RandomInRange * scale;
                IntVec3 desired = (river.ToVector3Shifted() + side * (bankSign * radius * Rand.Range(0.6f, 1.1f))).ToIntVec3();
                DesertPitRoom room = DesertPitRoomPlacement.Find(map, desired, radius, forbidden,
                    15f * scale, 0.42f, settings.outerBoundary - 0.04f, 0.94f);
                if (room == null) { missing++; bankSign = -bankSign; continue; }
                DesertPitRoomBrush.Apply(data, room, true);
                DesertPitRoomBrush.Connect(map, data, previous, room.Center, 3.4f * scale, forbidden);
                links.Add(room);
                previous = room.Center;
                bankSign = -bankSign;
            }

            count = settings.branchRoomCount.RandomInRange;
            //固定宿主快照，支洞不会再作为下一条支洞的宿主而形成无界扩张。
            int hostCount = links.Count;
            for (int i = 0; i < count; i++)
            {
                DesertPitRoom host = links[Mathf.Min(hostCount - 1, Mathf.FloorToInt((i + 0.4f) * hostCount / count))];
                float direction = (i % 2 == 0 ? 1f : -1f) * (positive ? 1f : -1f);
                float radius = settings.smallRoomRadius.RandomInRange * scale;
                IntVec3 desired = (host.Center.ToVector3Shifted() + side
                    * (direction * (Mathf.Min(host.RadiusX, host.RadiusZ) + radius + Rand.Range(3f, 8f) * scale))).ToIntVec3();
                DesertPitRoom branch = DesertPitRoomPlacement.Find(map, desired, radius, forbidden,
                    16f * scale, 0.6f, settings.innerBoundary, 0.93f);
                if (branch == null) { missing++; continue; }
                DesertPitRoomBrush.Apply(data, branch, true);
                DesertPitRoomBrush.Connect(map, data, host.Center, branch.Center, 2.8f * scale, forbidden);
            }
            return missing;
        }
    }
}
