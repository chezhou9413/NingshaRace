using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.Data;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：在保留生态区内生成紫辉花簇菌、坠砂裂隙与水潭岸边的地下热带树木。
    internal static class DesertPitHabitatContent
    {
        //函数职责：按生态区类型安排植物和裂隙，所有实体避开水面及通行核心。
        public static void Generate(Map map, DesertPitLayoutData data)
        {
            foreach (DesertPitHabitat habitat in data.Habitats)
            {
                List<IntVec3> dry = new List<IntVec3>(habitat.floor);
                dry.RemoveAll(cell => habitat.water.Contains(cell) || data.ProtectedRouteCells.Contains(cell));
                dry.Shuffle();
                if (habitat.marsh)
                    PlacePlants(map, dry, new[] { DefOfRefs.NingshaRace_DesertPitPlantD }, Mathf.Clamp(dry.Count / 3, 3, 10), 1.4f);
                if (habitat.pond)
                    PlacePlants(map, dry, new[] { DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitCecropia"),
                        DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitPalm") }, Rand.RangeInclusive(4, 8), 2.8f);
                if (habitat.sandfall) PlaceSandfalls(map, dry);
            }
        }

        //函数职责：只在满足原版生长与占地条件的干地生成指定植物，不强行覆盖场景实体。
        private static void PlacePlants(Map map, List<IntVec3> cells, ThingDef[] plants, int count, float spacing)
        {
            List<IntVec3> placed = new List<IntVec3>();
            foreach (IntVec3 cell in cells)
            {
                ThingDef plant = plants.RandomElement();
                if (!DesertPitPlantEcologyUtility.CanPlacePlant(map, cell, plant, false)) continue;
                if (placed.Exists(other => other.DistanceToSquared(cell) < spacing * spacing)) continue;
                DesertPitPlantEcologyUtility.SpawnPlant(map, plant, cell, new FloatRange(0.75f, 1f));
                placed.Add(cell);
                if (placed.Count >= count) return;
            }
            if (placed.Count == 0) throw new System.InvalidOperationException("生态区域没有可种植目标植物的干地：" + plants[0].defName);
        }

        //函数职责：把多处坠砂裂隙散布在洞室干地，保持入口和树木位置畅通。
        private static void PlaceSandfalls(Map map, List<IntVec3> cells)
        {
            ThingDef sandfall = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitCeilingSandfall");
            int target = Rand.RangeInclusive(8, 14);
            List<IntVec3> placed = new List<IntVec3>();
            foreach (IntVec3 cell in cells)
            {
                if (!cell.Standable(map) || cell.GetEdifice(map) != null || cell.GetPlant(map) != null
                    || placed.Exists(other => other.DistanceToSquared(cell) < 4f)) continue;
                GenSpawn.Spawn(sandfall, cell, map);
                placed.Add(cell);
                if (placed.Count >= target) return;
            }
        }
    }
}
