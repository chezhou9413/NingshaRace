using System;
using NingshaRaceLib.DesertPit.Generation.Data;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Hydrology
{
    //类职责：把预先开挖的河槽落实为连续浅水和干燥岸线，不按通道保护标记截断河流。
    internal static class DesertPitRiverPainter
    {
        //函数职责：在场景散布前绘制河道，发现被岩墙占用时直接报告生成顺序错误。
        public static void Paint(Map map, DesertPitLayoutData data)
        {
            TerrainDef water = DefDatabase<TerrainDef>.GetNamed("WaterMovingShallow");
            foreach (IntVec3 cell in data.RiverCorridorCells)
            {
                if (cell.GetEdifice(map) != null) throw new InvalidOperationException("预留河道被岩墙或建筑占用，水文生成顺序不正确。");
                map.terrainGrid.SetTerrain(cell, data.RiverWaterCells.Contains(cell) ? water : TerrainDefOf.Sand);
            }
        }
    }
}
