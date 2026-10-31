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
                || data.RiverCorridorCells.Contains(cell)
                || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map))
                || map.GetComponent<MapComponent_DesertPitAntColonies>().IsColonyStorageCell(cell)) return false;
            foreach (Thing thing in cell.GetThingList(map))
                if (thing is Building || thing is Plant || thing.def.category == ThingCategory.Item || thing is Pawn) return false;
            return true;
        }
    }
}
