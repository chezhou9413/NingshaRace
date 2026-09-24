using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Ecology.Config;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Generation
{
    //类职责：在普通菌群之外，按地图分区独立散布产木和结果巨菇。
    internal static class DesertPitGiantFungi
    {
        private const int RegionSize = 24;
        private const float ReferenceArea = 200f * 200f;

        //函数职责：按地图面积缩放各类巨菇配额，并记录空间不足时的实际生成数量。
        public static void Generate(Map map, DesertPitLayoutData data, DefModExtension_DesertPitEcology settings)
        {
            foreach (DesertPitGiantFungus fungus in settings.giantFungi)
            {
                int requested = fungus.countRange.RandomInRange;
                if (requested == 0) continue;
                int target = Mathf.Max(1,
                    Mathf.RoundToInt(requested * map.cellIndices.NumGridCells / ReferenceArea));
                List<List<IntVec3>> regions = CollectRegions(map, data, fungus);
                int candidates = 0;
                foreach (List<IntVec3> region in regions) candidates += region.Count;
                int placed = Scatter(map, fungus, regions, target);
                Log.Message("[凝砂族] " + fungus.plant.label + "地下生成，目标：" + target
                    + "，实际：" + placed + "，初始合法格：" + candidates + "。数量受空位与树木间距限制。");
            }
        }

        //函数职责：将天然洞室干地按二十四格网格分区，排除通道、入口、场景及菌巢温床。
        private static List<List<IntVec3>> CollectRegions(Map map, DesertPitLayoutData data, DesertPitGiantFungus fungus)
        {
            Dictionary<IntVec3, List<IntVec3>> lookup = new Dictionary<IntVec3, List<IntVec3>>();
            List<List<IntVec3>> regions = new List<List<IntVec3>>();
            MapComponent_AntHabitats habitats = map.GetComponent<MapComponent_AntHabitats>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!DesertPitGenUtility.IsCave(map, cell) || cell.DistanceToSquared(data.MainCenter) < 144f
                    || data.ProtectedRouteCells.Contains(cell) || data.PassageCells.Contains(cell)
                    || data.ReservedSceneCells.Contains(cell) || habitats.IsFungalHabitat(cell)
                    || !DesertPitPlantEcologyUtility.CanRegrowPlantAt(map, cell, fungus.plant)
                    || !DesertPitGiantFungusUtility.HasSpace(map, cell, fungus)) continue;

                IntVec3 regionKey = new IntVec3(cell.x / RegionSize, 0, cell.z / RegionSize);
                if (!lookup.TryGetValue(regionKey, out List<IntVec3> cells))
                {
                    cells = new List<IntVec3>();
                    lookup.Add(regionKey, cells);
                    regions.Add(cells);
                }
                cells.Add(cell);
            }
            foreach (List<IntVec3> region in regions) region.Shuffle();
            regions.Shuffle();
            return regions;
        }

        //函数职责：轮流从各分区取一个落点，耗尽区域退出循环，避免巨菇集中在同一洞室。
        private static int Scatter(Map map, DesertPitGiantFungus fungus, List<List<IntVec3>> regions, int target)
        {
            int placed = 0;
            while (placed < target && regions.Count > 0)
            {
                for (int i = regions.Count - 1; i >= 0 && placed < target; i--)
                {
                    if (TryPlaceInRegion(map, fungus, regions[i])) placed++;
                    else regions.RemoveAt(i);
                }
            }
            return placed;
        }

        //函数职责：取出仍满足间距的空格并生成混合成熟度植株，无合适落点时结束本分区。
        private static bool TryPlaceInRegion(Map map, DesertPitGiantFungus fungus, List<IntVec3> cells)
        {
            while (cells.Count > 0)
            {
                int last = cells.Count - 1;
                IntVec3 cell = cells[last];
                cells.RemoveAt(last);
                if (!DesertPitPlantEcologyUtility.CanRegrowPlantAt(map, cell, fungus.plant)
                    || !DesertPitGiantFungusUtility.HasSpace(map, cell, fungus)) continue;
                //部分植株可立即采收，其余植株沿用原版成长计算逐渐成熟。
                FloatRange growth = Rand.Chance(0.5f) ? new FloatRange(1f, 1f) : new FloatRange(0.35f, 0.85f);
                DesertPitPlantEcologyUtility.SpawnPlant(map, fungus.plant, cell, growth);
                return true;
            }
            return false;
        }
    }
}
