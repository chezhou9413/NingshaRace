using System.Collections;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Steps
{
    //类职责：把沙漠巨坑洞穴掩码落实成沙岩墙、厚岩顶、沙地、软沙和塌方地貌。
    public class GenStep_DesertPitTerrain : GenStep, IDesertPitIncrementalGenStep
    {
        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => 914027332;

        //函数职责：按洞穴掩码生成沙岩墙并铺设沙漠洞穴地形。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：分批铺设岩顶、地形和沙岩墙，避免单个生成步骤长时间占用主线程。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("砂岩地层");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            TerrainDef sandstoneRough = DefDatabase<TerrainDef>.GetNamed("Sandstone_Rough");
            ModuleBase rockNoise = new Perlin(0.055000000819563866, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);
            ModuleBase sandNoise = new Perlin(0.08500000089406967, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);
            RoofDef roofDef = map.generatorDef.roofDef;
            using (map.pathing.DisableIncrementalScope())
            {
                int processedCells = 0;
                foreach (IntVec3 cell in map.AllCells)
                {
                    map.roofGrid.SetRoof(cell, roofDef);
                    if (DesertPitGenUtility.IsCave(cell))
                    {
                        TerrainDef terrain = ChooseCaveTerrain(map, data, cell, rockNoise, sandNoise, sandstoneRough);
                        map.terrainGrid.SetTerrain(cell, terrain);
                    }
                    else
                    {
                        map.terrainGrid.SetTerrain(cell, TerrainDefOf.Sand);
                        GenSpawn.Spawn(ThingDefOf.Sandstone, cell, map);
                    }

                    processedCells++;
                    if (processedCells % 768 == 0)
                    {
                        DesertPitGenerationProgress.SetStepFraction((float)processedCells / map.cellIndices.NumGridCells);
                        yield return null;
                    }
                }

                PlaceCollapseRocks(map);
                DesertPitGenerationProgress.SetStepFraction(1f);
            }
        }

        //函数职责：根据洞壁、塌方、小洞室沉积和噪声选择砂岩、砾地、沙地或软沙。
        private static TerrainDef ChooseCaveTerrain(Map map, DesertPitLayoutData data, IntVec3 cell, ModuleBase rockNoise, ModuleBase sandNoise, TerrainDef sandstoneRough)
        {
            float rockValue = (float)rockNoise.GetValue(cell.x, 0.0, cell.z);
            float sandValue = (float)sandNoise.GetValue(cell.x, 0.0, cell.z);
            if (cell.DistanceTo(data.MainCenter) < 7f)
            {
                return sandValue > 0.55f ? TerrainDefOf.SoftSand : TerrainDefOf.Sand;
            }

            bool nearTightEdge = DesertPitGenUtility.NearCaveEdge(map, cell, 2);
            bool nearWideEdge = DesertPitGenUtility.NearCaveEdge(map, cell, 5);
            bool nearCollapse = NearCollapse(data, cell);
            bool nearSmallRoom = NearSmallRoom(data, cell);
            if (nearTightEdge)
            {
                return rockValue > -0.75f ? sandstoneRough : TerrainDefOf.Gravel;
            }

            if (nearCollapse)
            {
                return rockValue > -0.85f ? sandstoneRough : TerrainDefOf.Gravel;
            }

            if (nearWideEdge && rockValue > -0.45f)
            {
                return sandstoneRough;
            }

            if (nearWideEdge && rockValue > -0.85f)
            {
                return TerrainDefOf.Gravel;
            }

            if (nearSmallRoom && sandValue > 0.2f)
            {
                return sandValue > 0.6f ? TerrainDefOf.SoftSand : TerrainDefOf.Sand;
            }

            if (sandValue > 0.58f)
            {
                return TerrainDefOf.SoftSand;
            }

            if (sandValue > 0.16f)
            {
                return TerrainDefOf.Sand;
            }

            if (rockValue > 0.38f)
            {
                return sandstoneRough;
            }

            return rockValue > 0.05f ? TerrainDefOf.Gravel : TerrainDefOf.Sand;
        }

        //函数职责：在记录的塌方区域生成塌落岩和沙岩碎块。
        private static void PlaceCollapseRocks(Map map)
        {
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            ThingDef collapsedRock = DefOfRefs.NingshaRace_DesertPitCollapsedRockLarge;
            ThingDef sandstoneRubble = DefOfRefs.NingshaRace_DesertPitSandstoneRubbleSmall;
            foreach (IntVec3 collapse in data.Collapses)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(collapse, Rand.Range(3.5f, 6f), useCenter: true))
                {
                    if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(cell) || !cell.Standable(map))
                    {
                        continue;
                    }

                    if (Rand.Chance(0.24f) && !data.ProtectedRouteCells.Contains(cell) && cell.GetFirstThing(map, collapsedRock) == null)
                    {
                        GenSpawn.Spawn(collapsedRock, cell, map);
                    }
                    else if (Rand.Chance(0.32f) && cell.GetFirstThing(map, sandstoneRubble) == null)
                    {
                        GenSpawn.Spawn(sandstoneRubble, cell, map);
                    }

                    if (Rand.Chance(0.65f))
                    {
                        FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_RubbleRock, Rand.RangeInclusive(1, 2));
                    }
                }
            }
        }

        //函数职责：判断指定格子是否靠近塌方和碎石边缘。
        private static bool NearCollapse(DesertPitLayoutData data, IntVec3 cell)
        {
            for (int i = 0; i < data.Collapses.Count; i++)
            {
                if (cell.DistanceTo(data.Collapses[i]) <= 9f)
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：判断指定格子是否靠近记录的小洞室中心。
        private static bool NearSmallRoom(DesertPitLayoutData data, IntVec3 cell)
        {
            for (int i = 0; i < data.SmallRooms.Count; i++)
            {
                if (cell.DistanceTo(data.SmallRooms[i]) <= 12f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
