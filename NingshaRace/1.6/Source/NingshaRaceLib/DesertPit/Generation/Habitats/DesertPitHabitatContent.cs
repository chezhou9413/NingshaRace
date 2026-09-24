using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Lighting;
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
            DesertPitGenerationLighting.Refresh(map);
            foreach (DesertPitHabitat habitat in data.Habitats)
            {
                List<IntVec3> dry = new List<IntVec3>(habitat.floor);
                dry.RemoveAll(cell => habitat.water.Contains(cell) || data.ProtectedRouteCells.Contains(cell));
                dry.Shuffle();
                string label = "生态区域" + habitat.room.Center;
                if (habitat.marsh)
                    DesertPitHabitatPlants.Place(map, dry, new[] { DefOfRefs.NingshaRace_DesertPitPlantD }, Mathf.Clamp(dry.Count / 3, 6, 20), 1.4f, label);
                if (habitat.pond)
                {
                    List<IntVec3> shore = DesertPitHabitatShore.Collect(habitat, data);
                    int grassCount = Mathf.Max(4, shore.Count / 2);
                    DesertPitHabitatPlants.Place(map, shore, new[] { DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitCecropia"),
                        DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitPalm") }, Rand.RangeInclusive(4, 8), 2.8f, label + "岸带树木", true);
                    //树木可能参与遮光，草本选择前先提交它们对光照网格的影响。
                    DesertPitGenerationLighting.Refresh(map);
                    DesertPitHabitatPlants.Place(map, shore, new[] { DefDatabase<ThingDef>.GetNamed("NingshaRace_UndergroundGrass") },
                        grassCount, 1f, label + "岸带地下草", true);
                }
                if (habitat.sandfall) PlaceSandfalls(map, dry);
            }
        }

        //函数职责：把多处坠砂裂隙散布在洞室干地，保持入口和树木位置畅通。
        private static void PlaceSandfalls(Map map, List<IntVec3> cells)
        {
            ThingDef sandfall = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitCeilingSandfall");
            int target = Rand.RangeInclusive(8, 14);
            List<IntVec3> placed = new List<IntVec3>();
            foreach (IntVec3 cell in cells)
            {
                if (!Resources.DesertPitResourcePlacement.Empty(map, cell)
                    || placed.Exists(other => other.DistanceToSquared(cell) < 4f)) continue;
                GenSpawn.Spawn(sandfall, cell, map);
                placed.Add(cell);
                if (placed.Count >= target) return;
            }
        }
    }
}
