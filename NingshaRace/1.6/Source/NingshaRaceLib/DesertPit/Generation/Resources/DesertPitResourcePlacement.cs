using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Resources
{
    //类职责：按指定资源区的实际容量放置实体，保留真实生长基底与既有占地。
    internal static class DesertPitResourcePlacement
    {
        //函数职责：判断当前格是否为空闲洞穴干地，禁止覆盖建筑、植物、物品和生物。
        public static bool Empty(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map)
                || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map))) return false;
            foreach (Thing thing in cell.GetThingList(map))
                if (thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Item
                    || thing.def.category == ThingCategory.Plant || thing.def.category == ThingCategory.Pawn) return false;
            return true;
        }

        //函数职责：无重复抽取区域内的合法空格，容量不足时记录实际数量并按空位生成。
        public static List<IntVec3> TakeAvailable(Map map, IEnumerable<IntVec3> area, int count, string label)
        {
            List<IntVec3> cells = new List<IntVec3>();
            HashSet<IntVec3> region = new HashSet<IntVec3>(area);
            foreach (IntVec3 cell in region) if (Empty(map, cell)) cells.Add(cell);
            int actual = Math.Min(count, cells.Count);
            if (actual < count)
                Log.Message("[凝砂族] " + label + "按空位生成，目标：" + count + "，实际：" + actual
                    + "，区域格数：" + region.Count + "，合法空格：" + cells.Count + "。");
            cells.Shuffle();
            return cells.GetRange(0, actual);
        }

        //函数职责：为通道和连接口菌株铺设一格沙土沉积，确保岩石通道里的植物具备生长肥力。
        public static void Plant(Map map, IntVec3 cell, ThingDef plant)
        {
            if (map.fertilityGrid.FertilityAt(cell) < plant.plant.fertilityMin)
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Sand);
            if (!DesertPitPlantEcologyUtility.CanPlacePlant(map, cell, plant, false))
                throw new InvalidOperationException("地下资源区无法种植" + plant.label + "，坐标：" + cell);
            DesertPitPlantEcologyUtility.SpawnPlant(map, plant, cell, new FloatRange(1f, 1f));
        }

    }
}
