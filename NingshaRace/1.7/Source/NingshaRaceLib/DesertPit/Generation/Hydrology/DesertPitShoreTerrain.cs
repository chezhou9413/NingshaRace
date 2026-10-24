using System;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Steps;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Hydrology;

//沿实际水面铺设连续沙泥岸带，并登记后续生态需要避让的地面。
internal static class DesertPitShoreTerrain
{
    //水岸距离只沿地面扩展，岩壁会截断岸带，避免隔墙染湿另一间洞室。
    internal static void Paint(Map map, DesertPitLayoutData data)
    {
        var layout = data.Ecology;
        var distance = new int[layout.Cells.Length]; Array.Fill(distance, -1);
        var queue = new int[distance.Length]; int head = 0, tail = 0;
        for (int i = 0; i < distance.Length; i++)
            if (layout.Cells[i] >= 2) { distance[i] = 0; queue[tail++] = i; }
        while (head < tail)
        {
            int i = queue[head++];
            if (distance[i] >= 3) continue;
            foreach (int j in CaveRaster.Neighbors(i, layout.Size))
            {
                if (distance[j] >= 0 || layout.Cells[j] != 1 || layout.Reserved[j] != 0) continue;
                distance[j] = distance[i] + 1; queue[tail++] = j;
            }
        }

        uint seed = CaveMath.Hash(layout.Settings.Seed + ":shore-terrain");
        var marshy = DefDatabase<TerrainDef>.GetNamed("MarshyTerrain");
        for (int i = 0; i < distance.Length; i++)
        {
            if (distance[i] < 1 || layout.Walking[i] < 0) continue;
            var cell = layout.Cell(i);
            if (cell.DistanceToSquared(data.MainCenter) <= GenStep_DesertPitExit.SandRadius * GenStep_DesertPitExit.SandRadius) continue;
            double width = 2.5 + 1.2 * CaveMath.Noise(cell.x * .11, cell.z * .11, seed);
            if (distance[i] > width) continue;
            double wetness = CaveMath.Noise(cell.x * .055, cell.z * .055, seed + 71)
                + (layout.Oasis[i] == 3 ? .2 : 0);
            TerrainDef terrain;
            //受保护通道保持易行走沙地，贴水泥滩向外过渡为湿润土壤。
            if (data.ProtectedRouteCells.Contains(cell)) terrain = TerrainDefOf.Sand;
            else if (distance[i] == 1) terrain = wetness > .1 ? TerrainDefOf.Mud : TerrainDefOf.Sand;
            else if (wetness > .05) terrain = marshy;
            else terrain = distance[i] >= 3 && wetness < -.25 ? TerrainDefOf.Gravel : TerrainDefOf.Sand;
            map.terrainGrid.SetTerrain(cell, terrain);
            data.RiverCorridorCells.Add(cell);
        }
    }
}
