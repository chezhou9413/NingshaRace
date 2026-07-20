using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit
{
    //类职责：提供沙漠巨坑局部地貌使用的砂岩、砾地和软沙地面改造逻辑。
    public static class DesertPitLandmarkTerrainUtility
    {
        //函数职责：把石林区域地面改成砂岩、砾地和少量沙地混合的硬质沉积结构。
        public static void PaintStoneTerrain(Map map, IntVec3 center, float radius, TerrainDef sandstoneRough)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell))
                {
                    continue;
                }

                float distance = cell.DistanceTo(center) / radius;
                if (distance < 0.42f || DesertPitGenUtility.NearCaveEdge(map, cell, 3))
                {
                    map.terrainGrid.SetTerrain(cell, sandstoneRough);
                }
                else if (Rand.Chance(0.55f))
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.Gravel);
                }
            }
        }

        //函数职责：把水晶簇区域地面改成裂隙砂岩、砾地和软沙混合的渗水沉积结构。
        public static void PaintCrystalTerrain(Map map, IntVec3 center, float radius, TerrainDef sandstoneRough)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell))
                {
                    continue;
                }

                if (cell.DistanceTo(center) < radius * 0.45f || Rand.Chance(0.35f))
                {
                    map.terrainGrid.SetTerrain(cell, sandstoneRough);
                }
                else if (Rand.Chance(0.5f))
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.Gravel);
                }
            }
        }

        //函数职责：把坠砂区域地面改成塌陷后的砂岩、砾地和软沙混合结构。
        public static void PaintSandfallTerrain(Map map, IntVec3 center, float radius, TerrainDef sandstoneRough)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell))
                {
                    continue;
                }

                float distance = cell.DistanceTo(center) / radius;
                if (distance < 0.35f)
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.Gravel);
                }
                else if (Rand.Chance(0.45f))
                {
                    map.terrainGrid.SetTerrain(cell, sandstoneRough);
                }
                else if (Rand.Chance(0.35f))
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.SoftSand);
                }
            }
        }
    }
}
