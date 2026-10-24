using NingshaRaceLib.DesertPit.Generation.Data;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：把已规划的湿地、水潭和泥土岸带落实为地形，保留河道与通行核心。
    internal static class DesertPitHabitatTerrain
    {
        //函数职责：为每片生态区铺设水面和干地，沼泽噪声留下的干地全部铺为砂岩。
        public static void Paint(Map map, DesertPitLayoutData data)
        {
            TerrainDef marsh = DefDatabase<TerrainDef>.GetNamed("Marsh");
            TerrainDef shallow = DefDatabase<TerrainDef>.GetNamed("WaterShallow");
            TerrainDef rock = DefDatabase<TerrainDef>.GetNamed("NingshaRace_DesertPitMarshRock");
            foreach (DesertPitHabitat habitat in data.Habitats)
            {
                foreach (IntVec3 cell in habitat.floor)
                {
                    if (data.RiverCorridorCells.Contains(cell)) continue;
                    if (habitat.marsh)
                        map.terrainGrid.SetTerrain(cell, habitat.water.Contains(cell) ? marsh : rock);
                    else if (habitat.pond)
                        map.terrainGrid.SetTerrain(cell, habitat.water.Contains(cell) ? shallow : TerrainDefOf.Soil);
                }
            }
        }
    }
}
