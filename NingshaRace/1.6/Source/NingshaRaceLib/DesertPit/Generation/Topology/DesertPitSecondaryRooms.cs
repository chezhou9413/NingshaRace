using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Hydrology;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Topology
{
    //类职责：沿斜向河谷安置次要洞室及两岸错落洞群，保留岩脊、收窄洞口与独立侧室。
    internal static class DesertPitSecondaryRooms
    {
        //函数职责：构建一端必要次洞和有限连接洞，再安排外围支洞，返回空间不足的可选洞数。
        public static int Generate(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings, bool positive)
        {
            HashSet<IntVec3> forbidden = DesertPitRoomBrush.AntFootprints(data);
            IntVec3 crossing = FindRiverPoint(map, data, settings.outerBoundary, positive);
            float scale = settings.Scale(map);
            float radius = settings.secondaryRoomRadius.RandomInRange * scale;
            DesertPitRoom secondary = DesertPitRoomPlacement.Find(map, crossing, radius, forbidden,
                18f * scale, 0.3f, settings.outerBoundary - 0.1f, settings.outerBoundary + 0.1f);
            if (secondary == null) throw new InvalidOperationException("斜向河谷无法容纳必要的次要洞室，请检查地图尺寸和洞室参数。");
            DesertPitRoomBrush.Apply(data, secondary, true);
            data.SecondaryRooms.Add(secondary);

            int count = settings.linkRoomCount.RandomInRange;
            int missing = 0;
            Vector3 side = DesertPitRiverPlanner.Side(data);
            IntVec3 previous = data.MainCenter;
            float phase = Rand.Bool ? 1f : -1f;
            List<DesertPitRoom> links = new List<DesertPitRoom>();
            for (int i = 0; i < count; i++)
            {
                float layer = Mathf.Lerp(settings.innerBoundary + 0.04f, settings.outerBoundary - 0.06f, (i + 0.5f) / count);
                IntVec3 river = FindRiverPoint(map, data, layer, positive);
                IntVec3 desired = (river.ToVector3Shifted() + side * (Rand.Range(13f, 23f) * scale * phase)).ToIntVec3();
                float smallRadius = settings.smallRoomRadius.RandomInRange * scale;
                DesertPitRoom link = DesertPitRoomPlacement.Find(map, desired, smallRadius, forbidden, 18f * scale, 0.45f);
                phase = -phase;
                if (link == null) { missing++; continue; }
                DesertPitRoomBrush.Apply(data, link, true);
                DesertPitRoomBrush.Connect(map, data, previous, link.Center, 3.8f * scale, forbidden);
                links.Add(link);
                previous = link.Center;
            }
            DesertPitRoomBrush.Connect(map, data, previous, secondary.Center, 4.2f * scale, forbidden);
            links.Add(secondary);
            return missing + DesertPitValleyExtensions.Generate(map, data, settings, positive, secondary, links, forbidden);
        }

        //函数职责：在指定半边河流上按层位选择锚点，避免以河流索引代替实际地图距离而挤在中央。
        internal static IntVec3 FindRiverPoint(Map map, DesertPitLayoutData data, float layer, bool positive)
        {
            IntVec3 best = IntVec3.Invalid;
            float bestScore = float.MaxValue;
            int middle = DesertPitRiverPlanner.Axis(data.MainCenter, data.RiverRunsNorthSouth);
            foreach (IntVec3 cell in data.RiverCenterline)
            {
                if ((DesertPitRiverPlanner.Axis(cell, data.RiverRunsNorthSouth) > middle) != positive) continue;
                float score = Mathf.Abs(DefModExtension_DesertPitLayout.Layer(map, cell) - layer);
                if (score < bestScore) { bestScore = score; best = cell; }
            }
            if (!best.IsValid) throw new InvalidOperationException("斜向河流缺少指定半边的洞群锚点。");
            return best;
        }
    }
}
