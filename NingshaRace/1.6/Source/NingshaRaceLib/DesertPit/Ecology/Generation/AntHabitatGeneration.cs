using System;
using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Generation
{
    //类职责：在蚁巢与入口周围安置生态建筑、富含有机质的菌床及初始食用菌。
    public static class AntHabitatGeneration
    {
        //函数职责：生成菌巢并在有效栖息格铺设土壤，不改写厚壁、储藏和通道。
        public static void SpawnMound(Map map, DesertPitLayoutData data, IntVec3 center)
        {
            Building_FungalMound mound = (Building_FungalMound)GenSpawn.Spawn(DefOfRefs.NingshaRace_FungalMound, center, map);
            MapComponent_DesertPitAntColonies ants = map.GetComponent<MapComponent_DesertPitAntColonies>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, mound.Radius, true))
            {
                if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell)) continue;
                data.ReservedSceneCells.Add(cell);
                if (cell.Standable(map) && cell.GetEdifice(map) == null && !data.ProtectedRouteCells.Contains(cell)
                    && !ants.IsColonyStorageCell(cell) && !DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)))
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            }
            mound.SeedHabitat(data.ProtectedRouteCells);
        }

        //函数职责：从可用洞穴地面中选出一处能容纳食用菌群的位置，任务巢区允许使用自身预留地。
        public static IntVec3 FindMoundCell(Map map, DesertPitLayoutData data, IntVec3 origin, float minDistance, float maxDistance, bool withinNestScene)
        {
            IntVec3 best = IntVec3.Invalid;
            float bestScore = float.MinValue;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, maxDistance, true))
            {
                float distance = cell.DistanceTo(origin);
                if (distance < minDistance || !CanPlace(map, data, cell, DefOfRefs.NingshaRace_FungalMound, withinNestScene)) continue;
                int growingCells = 0;
                foreach (IntVec3 nearby in GenRadial.RadialCellsAround(cell, 5f, true))
                    if (CanUseGround(map, data, nearby, withinNestScene)) growingCells++;
                if (growingCells < 36) continue;
                float score = growingCells - distance * 0.2f;
                if (score > bestScore) { bestScore = score; best = cell; }
            }
            if (!best.IsValid) throw new InvalidOperationException("洞穴生态生成无法找到足够宽敞的菌巢栖息地。");
            return best;
        }

        //函数职责：在指定位置附近放置可占领的驱蚁桩，避免占用离洞绳和其他建筑。
        public static void SpawnRepellent(Map map, DesertPitLayoutData data, IntVec3 origin, float minDistance, float maxDistance, bool allowReserved = false)
        {
            List<IntVec3> candidates = new List<IntVec3>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, maxDistance, true))
                if (cell.DistanceTo(origin) >= minDistance && CanPlace(map, data, cell, DefOfRefs.NingshaRace_AntRepellent, allowReserved)) candidates.Add(cell);
            if (candidates.Count == 0) throw new InvalidOperationException("洞穴中没有可放置驱蚁桩的位置。");
            IntVec3 position = candidates.RandomElement();
            GenSpawn.Spawn(DefOfRefs.NingshaRace_AntRepellent, position, map);
            data.ReservedSceneCells.Add(position);
        }

        //函数职责：检查完整建筑占地，防止天然菌巢覆盖已有实体或预留通道。
        internal static bool CanPlace(Map map, DesertPitLayoutData data, IntVec3 center, ThingDef def, bool withinNestScene)
        {
            foreach (IntVec3 cell in GenAdj.OccupiedRect(center, Rot4.North, def.size))
                if (!CanUseGround(map, data, cell, withinNestScene)) return false;
            return true;
        }

        //函数职责：识别未被建筑、植物、物品及巢群储藏占用的干燥洞穴地面。
        internal static bool CanUseGround(Map map, DesertPitLayoutData data, IntVec3 cell, bool withinNestScene)
        {
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map)
                || data.ProtectedRouteCells.Contains(cell) || !withinNestScene && data.ReservedSceneCells.Contains(cell)
                || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map))
                || map.GetComponent<MapComponent_DesertPitAntColonies>().IsColonyStorageCell(cell)) return false;
            foreach (Thing thing in cell.GetThingList(map))
                if (thing is Building || thing is Plant || thing.def.category == ThingCategory.Item || thing is Pawn) return false;
            return true;
        }
    }
}
