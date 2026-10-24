using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.PocketMaps.Generation
{
    //类职责：在口袋地图尚未完成初始化时批量写入统一屋顶和地形，避免逐格触发无效的绘制与环境通知。
    internal static class PocketMapInitialGridUtility
    {
        //字段职责：缓存原版屋顶底层数组入口，仅用于尚未完成初始化的新地图。
        private static readonly FieldInfo RoofGridField = AccessTools.Field(typeof(RoofGrid), "roofGrid");

        //函数职责：直接填充新地图的统一屋顶数组，后续FinalizeInit负责首次完整网格构建。
        public static void FillUniformRoof(Map map, RoofDef roof)
        {
            RoofDef[] grid = RoofGridField?.GetValue(map.roofGrid) as RoofDef[];
            if (grid == null || grid.Length != map.cellIndices.NumGridCells)
            {
                throw new InvalidOperationException("凝砂口袋地图无法访问原版屋顶底层网格。");
            }
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = roof;
            }
        }

        //函数职责：直接填充新地图的统一表层地形，保留尚未写入的地基、底层和临时地形数组。
        public static void FillUniformTerrain(Map map, TerrainDef terrain)
        {
            TerrainDef[] grid = map.terrainGrid.topGrid;
            if (grid == null || grid.Length != map.cellIndices.NumGridCells)
            {
                throw new InvalidOperationException("凝砂口袋地图无法访问原版地形底层网格。");
            }
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = terrain;
            }
        }
    }
}
