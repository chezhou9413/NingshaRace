using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Lighting;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：按生态植物配额选择合法落点，并确保地下树草满足真实生长光照。
    internal static class DesertPitHabitatPlants
    {
        //函数职责：在真实容量内规划互不覆盖的植物格，保持光照、肥力和间距要求后按实际数量生成。
        public static void Place(Map map, List<IntVec3> cells, ThingDef[] plants, int count,
            float spacing, string label, bool requireLight = false)
        {
            if (count <= 0) return;
            List<IntVec3> candidates = new List<IntVec3>(cells);
            candidates.Shuffle();
            List<KeyValuePair<IntVec3, ThingDef>> chosen = new List<KeyValuePair<IntVec3, ThingDef>>();
            foreach (IntVec3 cell in candidates)
            {
                if (requireLight && map.glowGrid.GroundGlowAt(cell) < 0.5f) continue;
                if (chosen.Exists(entry => entry.Key.DistanceToSquared(cell) < spacing * spacing)) continue;
                List<ThingDef> allowed = new List<ThingDef>();
                foreach (ThingDef plant in plants)
                    if (DesertPitPlantEcologyUtility.CanPlacePlant(map, cell, plant, false)) allowed.Add(plant);
                if (allowed.Count == 0) continue;
                chosen.Add(new KeyValuePair<IntVec3, ThingDef>(cell, allowed.RandomElement()));
                if (chosen.Count == count) break;
            }
            if (chosen.Count < count)
                Log.Message("[凝砂族] " + label + "按可用落点生成，目标：" + count + "，实际：" + chosen.Count
                    + "，符合肥力、光照和间距的可用落点：" + chosen.Count + "。保留生态区域并继续生成。");
            foreach (var entry in chosen)
                DesertPitPlantEcologyUtility.SpawnPlant(map, entry.Value, entry.Key, new FloatRange(0.75f, 1f));
        }

        //函数职责：在所有资源生成后刷新实际光照，确认地下树草没有因后续遮挡失去生长条件。
        public static void ValidateLight(Map map, DesertPitLayoutData data)
        {
            DesertPitGenerationLighting.Refresh(map);
            ThingDef cecropia = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitCecropia");
            ThingDef palm = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitPalm");
            ThingDef grass = DefDatabase<ThingDef>.GetNamed("NingshaRace_UndergroundGrass");
            foreach (DesertPitHabitat habitat in data.Habitats)
            {
                if (!habitat.pond) continue;
                foreach (IntVec3 cell in habitat.floor)
                {
                    var plant = cell.GetPlant(map);
                    if (plant == null || (plant.def != cecropia && plant.def != palm && plant.def != grass)) continue;
                    float light = map.glowGrid.GroundGlowAt(cell);
                    if (light < 0.5f)
                        throw new InvalidOperationException("生态洞室" + habitat.room.Center + "中的" + plant.Label
                            + "光照不足，位置：" + cell + "，最低：50%，实际：" + light.ToStringPercent());
                }
            }
        }
    }
}
