using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Config;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Topology
{
    //类职责：在有限局部候选中安置完整洞室，以实际地面重叠量防止洞群挤成没有轮廓的大空场。
    internal static class DesertPitRoomPlacement
    {
        //函数职责：复用洞壁形状寻找可容纳的位置，无合格位置时由调用者决定必要结构报错或可选结构减量。
        public static DesertPitRoom Find(Map map, IntVec3 desired, float radius, HashSet<IntVec3> forbidden,
            float searchRadius, float minNewFloor, float minLayer = 0f, float maxLayer = 1f)
        {
            DesertPitCaveProfile profile = new DesertPitCaveProfile(radius, radius * Rand.Range(0.72f, 0.98f), Rand.Range(0f, 180f));
            float phase = Rand.Range(0f, 360f);
            for (int attempt = 0; attempt < 32; attempt++)
            {
                //黄金角向外展开候选，不反复碰运气采样同一片被蚁巢占据的区域。
                Vector3 offset = Vector3Utility.FromAngleFlat(phase + attempt * 137.508f)
                    * (Mathf.Sqrt(attempt / 31f) * searchRadius);
                IntVec3 center = (desired.ToVector3Shifted() + offset).ToIntVec3();
                if (!center.InBounds(map) || forbidden.Contains(center) || center.DistanceToEdge(map) < radius * 0.55f) continue;
                bool blockedEntrance = false;
                foreach (IntVec3 direction in GenAdj.CardinalDirections)
                    if (forbidden.Contains(center + direction)) { blockedEntrance = true; break; }
                if (blockedEntrance) continue;
                float layer = DefModExtension_DesertPitLayout.Layer(map, center);
                if (layer < minLayer || layer > maxLayer) continue;
                DesertPitRoom room = DesertPitRoomBrush.Create(map, center, profile, forbidden);
                if (room.Floor.Count < Mathf.PI * profile.RadiusX * profile.RadiusZ * 0.72f) continue;
                int uncarved = 0;
                foreach (IntVec3 cell in room.Floor)
                    if (MapGenerator.Caves[cell] <= 0f) uncarved++;
                if (uncarved < room.Floor.Count * minNewFloor) continue;
                return room;
            }
            return null;
        }
    }
}
