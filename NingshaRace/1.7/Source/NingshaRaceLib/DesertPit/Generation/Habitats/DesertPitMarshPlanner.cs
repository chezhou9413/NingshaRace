using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Hydrology;
using NingshaRaceLib.DesertPit.Generation.Topology;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：沿河岸规划不规则湿地，按实际区域面积划分沼泽和成片干地。
    internal static class DesertPitMarshPlanner
    {
        //函数职责：选择互不重叠的沿河湿地并预留占地，避免后续地貌覆盖。
        public static void Generate(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            int count = settings.marshCount.RandomInRange;
            HashSet<IntVec3> forbidden = DesertPitRoomBrush.AntFootprints(data);
            foreach (DesertPitHabitat habitat in data.Habitats) forbidden.UnionWith(habitat.floor);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(data.MainCenter, settings.mainDryRadius + 2f, true)) forbidden.Add(cell);
            List<IntVec3> candidates = new List<IntVec3>(data.RiverCenterline);
            candidates.Shuffle();
            for (int i = 0; i < count; i++)
            {
                float radius = settings.marshRadius.RandomInRange;
                DesertPitHabitat habitat = Find(map, data, candidates, radius, forbidden);
                if (habitat == null) throw new InvalidOperationException("沿河无法容纳指定数量的沼泽，目标：" + count + "，已生成：" + i);
                DesertPitRoomBrush.Apply(data, habitat.room, false);
                SelectWetCells(habitat, settings.marshCoverage);
                data.Habitats.Add(habitat);
                data.ReservedSceneCells.UnionWith(habitat.floor);
                forbidden.UnionWith(habitat.floor);
                candidates.RemoveAll(cell => cell.DistanceTo(habitat.room.Center) < radius + settings.marshRadius.max + 6f);
            }
        }

        //函数职责：让湿地轮廓接触既有河岸，并避开蚁巢、出生地和其他生态洞室。
        private static DesertPitHabitat Find(Map map, DesertPitLayoutData data, List<IntVec3> candidates,
            float radius, HashSet<IntVec3> forbidden)
        {
            Vector3 side = DesertPitRiverPlanner.Side(data);
            foreach (IntVec3 river in candidates)
            {
                if (river.DistanceToEdge(map) < radius + 4f) continue;
                float sign = Rand.Bool ? 1f : -1f;
                for (int bank = 0; bank < 2; bank++)
                {
                    IntVec3 center = (river.ToVector3Shifted() + side * (radius * 0.65f + 2f) * sign).ToIntVec3();
                    sign = -sign;
                    DesertPitRoom room = DesertPitRoomBrush.Create(map, center, radius, radius * 0.9f, Rand.Range(0f, 180f), forbidden);
                    if (room.Floor.Count < Mathf.PI * radius * radius * 0.7f) continue;
                    bool touchesRiver = false;
                    foreach (IntVec3 cell in room.Floor)
                        if (data.RiverCorridorCells.Contains(cell)) { touchesRiver = true; break; }
                    if (!touchesRiver) continue;
                    DesertPitHabitat habitat = new DesertPitHabitat { room = room, marsh = true };
                    habitat.floor.UnionWith(room.Floor);
                    habitat.floor.ExceptWith(data.ReservedSceneCells);
                    habitat.floor.ExceptWith(data.ProtectedRouteCells);
                    if (habitat.floor.Count < Mathf.PI * radius * radius * 0.4f) continue;
                    return habitat;
                }
            }
            return null;
        }

        //函数职责：按空间噪声排序形成干湿斑块，并把沼泽面积保持为配置比例的最接近整数格。
        private static void SelectWetCells(DesertPitHabitat habitat, float coverage)
        {
            ModuleBase noise = new Perlin(0.16, 2.0, 0.5, 2, Rand.Int, QualityMode.Medium);
            List<IntVec3> cells = new List<IntVec3>(habitat.floor);
            cells.Sort((a, b) => noise.GetValue(a.x, 0, a.z).CompareTo(noise.GetValue(b.x, 0, b.z)));
            int wet = Mathf.RoundToInt(cells.Count * coverage);
            for (int i = 0; i < wet; i++) habitat.water.Add(cells[i]);
        }
    }
}
