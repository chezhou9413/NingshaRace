using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Steps
{
    //类职责：在沙漠巨坑洞穴中生成石林、水晶簇和坠砂塌陷带等局部地貌。
    public class GenStep_DesertPitLandmarks : GenStep
    {
        //字段职责：记录当前生成步骤使用的稳定随机种子片段。
        private const int Seed = 914027339;

        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：选择局部地貌中心并依次生成石林、水晶簇和坠砂塌陷带。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("洞穴地貌");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            TerrainDef sandstoneRough = DefDatabase<TerrainDef>.GetNamed("Sandstone_Rough");
            ThingDef sandstoneChunk = DefDatabase<ThingDef>.GetNamed("ChunkSandstone");
            ThingDef sandfallDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitCeilingSandfall");
            List<IntVec3> centers = DesertPitLandmarkUtility.CollectCenterCandidates(map, data);
            if (centers.Count == 0)
            {
                return;
            }

            ScatterStoneForests(map, data, centers, sandstoneRough, sandstoneChunk);
            ScatterCrystalGroves(map, data, centers, sandstoneRough, sandstoneChunk);
            ScatterSandfallFields(map, data, centers, sandstoneRough, sandstoneChunk, sandfallDef);
        }

        //函数职责：生成多片以钟乳石残柱为主体的密集石林地貌。
        private static void ScatterStoneForests(Map map, DesertPitLayoutData data, List<IntVec3> centers, TerrainDef sandstoneRough, ThingDef sandstoneChunk)
        {
            int count = Mathf.Min(Rand.RangeInclusive(3, 5), centers.Count);
            for (int i = 0; i < count; i++)
            {
                IntVec3 center;
                if (!DesertPitLandmarkUtility.TryTakeCenter(map, data, centers, 15f, out center))
                {
                    return;
                }

                float radius = Rand.Range(7.5f, 11.5f);
                DesertPitLandmarkTerrainUtility.PaintStoneTerrain(map, center, radius, sandstoneRough);
                PlaceStalactiteForest(map, data, center, radius);
                DesertPitLandmarkUtility.ScatterRubble(map, center, radius, Rand.RangeInclusive(42, 70));
                DesertPitLandmarkUtility.ScatterChunks(map, center, radius, sandstoneChunk, Rand.RangeInclusive(4, 8));
            }
        }

        //函数职责：生成沿小洞室和洞壁裂隙集中的发光水晶簇地貌。
        private static void ScatterCrystalGroves(Map map, DesertPitLayoutData data, List<IntVec3> centers, TerrainDef sandstoneRough, ThingDef sandstoneChunk)
        {
            int count = Mathf.Min(Rand.Chance(0.65f) ? 1 : 2, centers.Count);
            for (int i = 0; i < count; i++)
            {
                IntVec3 center;
                if (!DesertPitLandmarkUtility.TryTakeCenter(map, data, centers, 13f, out center))
                {
                    return;
                }

                float radius = Rand.Range(4.8f, 6.8f);
                DesertPitLandmarkTerrainUtility.PaintCrystalTerrain(map, center, radius, sandstoneRough);
                PlaceCrystalCluster(map, data, center, radius);
                DesertPitLandmarkUtility.ScatterRubble(map, center, radius, Rand.RangeInclusive(14, 24));
                DesertPitLandmarkUtility.ScatterChunks(map, center, radius, sandstoneChunk, Rand.RangeInclusive(2, 5));
            }
        }

        //函数职责：生成带有坠砂特效、塌落岩和岩屑的活动塌陷带。
        private static void ScatterSandfallFields(Map map, DesertPitLayoutData data, List<IntVec3> centers, TerrainDef sandstoneRough, ThingDef sandstoneChunk, ThingDef sandfallDef)
        {
            int count = Mathf.Min(Rand.RangeInclusive(2, 3), centers.Count);
            for (int i = 0; i < count; i++)
            {
                IntVec3 center;
                if (!DesertPitLandmarkUtility.TryTakeCenter(map, data, centers, 12f, out center))
                {
                    return;
                }

                float radius = Rand.Range(6f, 10f);
                DesertPitLandmarkTerrainUtility.PaintSandfallTerrain(map, center, radius, sandstoneRough);
                PlaceSandfallEmitters(map, data, center, radius, sandfallDef);
                PlaceCollapseRocks(map, data, center, radius);
                DesertPitLandmarkUtility.ScatterRubble(map, center, radius, Rand.RangeInclusive(52, 86));
                DesertPitLandmarkUtility.ScatterChunks(map, center, radius, sandstoneChunk, Rand.RangeInclusive(4, 9));
            }
        }

        //函数职责：在石林区域密集放置不同形态的钟乳石残柱。
        private static void PlaceStalactiteForest(Map map, DesertPitLayoutData data, IntVec3 center, float radius)
        {
            List<IntVec3> placed = new List<IntVec3>();
            List<IntVec3> candidates = DesertPitLandmarkPlacementUtility.CollectLocalCandidates(map, data, center, radius);
            int target = Mathf.Min(Rand.RangeInclusive(42, 62), candidates.Count);
            int guard = 0;
            while (placed.Count < target && candidates.Count > 0 && guard < target * 5)
            {
                ThingDef def = DesertPitDecorationUtility.ChooseStalactiteDef();
                IntVec3 cell;
                if (DesertPitLandmarkPlacementUtility.TryTakeLocalCell(map, center, radius, candidates, placed, def, out cell))
                {
                    GenSpawn.Spawn(def, cell, map);
                    placed.Add(cell);
                }

                guard++;
            }
        }

        //函数职责：在水晶簇区域放置不同尺寸的发光砂晶。
        private static void PlaceCrystalCluster(Map map, DesertPitLayoutData data, IntVec3 center, float radius)
        {
            List<IntVec3> placed = new List<IntVec3>();
            List<IntVec3> candidates = DesertPitLandmarkPlacementUtility.CollectLocalCandidates(map, data, center, radius);
            int target = Mathf.Min(Rand.RangeInclusive(6, 12), candidates.Count);
            int guard = 0;
            while (placed.Count < target && candidates.Count > 0 && guard < target * 5)
            {
                ThingDef def = DesertPitDecorationUtility.ChooseCrystalDef();
                IntVec3 cell;
                if (DesertPitLandmarkPlacementUtility.TryTakeLocalCell(map, center, radius, candidates, placed, def, out cell))
                {
                    GenSpawn.Spawn(def, cell, map);
                    placed.Add(cell);
                }

                guard++;
            }
        }

        //函数职责：在塌陷带中放置持续播放洞顶坠砂效果的不可见发射器。
        private static void PlaceSandfallEmitters(Map map, DesertPitLayoutData data, IntVec3 center, float radius, ThingDef sandfallDef)
        {
            List<IntVec3> placed = new List<IntVec3>();
            List<IntVec3> candidates = DesertPitLandmarkPlacementUtility.CollectLocalCandidates(map, data, center, radius);
            int target = Rand.RangeInclusive(4, 7);
            int guard = 0;
            while (placed.Count < target && candidates.Count > 0 && guard < target * 5)
            {
                IntVec3 cell;
                if (DesertPitLandmarkPlacementUtility.TryTakeLocalCell(map, center, radius, candidates, placed, sandfallDef, out cell))
                {
                    GenSpawn.Spawn(sandfallDef, cell, map);
                    placed.Add(cell);
                }

                guard++;
            }
        }

        //函数职责：在坠砂区域放置少量塌落岩块，表现近期崩塌痕迹。
        private static void PlaceCollapseRocks(Map map, DesertPitLayoutData data, IntVec3 center, float radius)
        {
            int count = Rand.RangeInclusive(1, 3);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                if (CellFinder.TryFindRandomCellNear(center, map, Mathf.CeilToInt(radius), (IntVec3 candidate) => DesertPitLandmarkUtility.CanPlaceLandmarkThing(map, data, candidate), out cell))
                {
                    GenSpawn.Spawn(ThingDefOf.CollapsedRocks, cell, map);
                }
            }
        }
    }
}
