using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Ecology.Config;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Steps
{
    //类职责：在沙漠巨坑洞穴中按簇群散布凝砂发光植物和原版洞穴植物。
    public class GenStep_DesertPitPlants : GenStep
    {
        //字段职责：记录当前生成步骤使用的稳定随机种子片段。
        private const int Seed = 914027336;

        //字段职责：限制入口周围植物生成，避免堵住玩家进入点。
        private const float MainSafeRadius = 8f;

        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：收集洞穴候选格并按中等密度簇群生成装饰植物。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("洞穴植物");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            DefModExtension_DesertPitEcology settings = DesertPitPlantEcologyUtility.GetSettings(map);
            ThingDef glowDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlow");
            List<IntVec3> candidates = CollectCandidates(map, data, glowDef);
            if (candidates.Count == 0)
            {
                return;
            }

            List<IntVec3> placed = new List<IntVec3>();
            int targetCount = Mathf.Min(Rand.RangeInclusive(70, 105), candidates.Count);
            int clusterCount = Mathf.Min(Rand.RangeInclusive(16, 24), candidates.Count);
            for (int i = 0; i < clusterCount && placed.Count < targetCount && candidates.Count > 0; i++)
            {
                IntVec3 center = candidates.RandomElementByWeight((IntVec3 cell) => ClusterCenterWeight(map, data, cell));
                ScatterCluster(map, data, settings, candidates, placed, center, targetCount);
            }

            FillRemainingPlants(map, data, settings, candidates, placed, targetCount);
        }

        //函数职责：收集所有可放置洞穴植物的基础候选格。
        private static List<IntVec3> CollectCandidates(Map map, DesertPitLayoutData data, ThingDef glowDef)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanPlacePlant(map, data, glowDef, cell))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        //函数职责：在指定簇心附近生成三到七株混合植物。
        private static void ScatterCluster(Map map, DesertPitLayoutData data, DefModExtension_DesertPitEcology settings, List<IntVec3> candidates, List<IntVec3> placed, IntVec3 center, int targetCount)
        {
            int count = Rand.RangeInclusive(3, 7);
            float radius = Rand.Range(3.5f, 7f);
            for (int i = 0; i < count && placed.Count < targetCount; i++)
            {
                ThingDef plantDef = DesertPitPlantEcologyUtility.ChoosePlantDef(settings);
                bool largePlant = IsLargePlant(plantDef);
                IntVec3 cell;
                if (TryFindClusterCell(map, data, candidates, placed, center, radius, plantDef, largePlant, out cell))
                {
                    DesertPitPlantEcologyUtility.SpawnPlant(map, plantDef, cell, new FloatRange(0.72f, 1f));
                    placed.Add(cell);
                    candidates.Remove(cell);
                }
            }
        }

        //函数职责：在簇群未达到目标数量时补少量零散植物。
        private static void FillRemainingPlants(Map map, DesertPitLayoutData data, DefModExtension_DesertPitEcology settings, List<IntVec3> candidates, List<IntVec3> placed, int targetCount)
        {
            int guard = 0;
            while (placed.Count < targetCount && candidates.Count > 0 && guard < 300)
            {
                ThingDef plantDef = DesertPitPlantEcologyUtility.ChoosePlantDef(settings);
                bool largePlant = IsLargePlant(plantDef);
                IntVec3 cell;
                if (TryFindAnyCell(map, data, candidates, placed, plantDef, largePlant, out cell))
                {
                    DesertPitPlantEcologyUtility.SpawnPlant(map, plantDef, cell, new FloatRange(0.72f, 1f));
                    placed.Add(cell);
                    candidates.Remove(cell);
                }
                else
                {
                    break;
                }

                guard++;
            }
        }

        //函数职责：从指定簇范围内选择一个符合间距要求的植物格。
        private static bool TryFindClusterCell(Map map, DesertPitLayoutData data, List<IntVec3> candidates, List<IntVec3> placed, IntVec3 center, float radius, ThingDef plantDef, bool largePlant, out IntVec3 cell)
        {
            List<IntVec3> localCandidates = new List<IntVec3>();
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 candidate = candidates[i];
                if (candidate.DistanceTo(center) <= radius && SpacingAllows(placed, candidate, largePlant) && DesertPitPlantEcologyUtility.CanPlacePlant(map, candidate, plantDef, false))
                {
                    localCandidates.Add(candidate);
                }
            }

            if (localCandidates.Count == 0)
            {
                cell = IntVec3.Invalid;
                return false;
            }

            cell = localCandidates.RandomElementByWeight((IntVec3 candidate) => CellPlacementWeight(map, data, candidate));
            return true;
        }

        //函数职责：从剩余候选格中选择一个满足当前植物尺寸间距要求的位置。
        private static bool TryFindAnyCell(Map map, DesertPitLayoutData data, List<IntVec3> candidates, List<IntVec3> placed, ThingDef plantDef, bool largePlant, out IntVec3 cell)
        {
            List<IntVec3> localCandidates = new List<IntVec3>();
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 candidate = candidates[i];
                if (SpacingAllows(placed, candidate, largePlant) && DesertPitPlantEcologyUtility.CanPlacePlant(map, candidate, plantDef, false))
                {
                    localCandidates.Add(candidate);
                }
            }

            if (localCandidates.Count == 0)
            {
                cell = IntVec3.Invalid;
                return false;
            }

            cell = localCandidates.RandomElementByWeight((IntVec3 candidate) => CellPlacementWeight(map, data, candidate));
            return true;
        }

        //函数职责：判断格子是否满足基础占用、洞穴、安全区和地貌条件。
        private static bool CanPlacePlant(Map map, DesertPitLayoutData data, ThingDef glowDef, IntVec3 cell)
        {
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map) || data.ReservedSceneCells.Contains(cell))
            {
                return false;
            }

            if (DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)))
            {
                return false;
            }

            if (cell.DistanceTo(data.MainCenter) < MainSafeRadius || cell.GetEdifice(map) != null || cell.GetPlant(map) != null)
            {
                return false;
            }

            if (cell.GetFirstThing(map, glowDef) != null)
            {
                return false;
            }

            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Plant)
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：判断新植物与已放置植物之间是否保留足够间隔。
        private static bool SpacingAllows(List<IntVec3> placed, IntVec3 cell, bool largePlant)
        {
            float minDistance = largePlant ? 1.35f : 1f;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].DistanceTo(cell) < minDistance)
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：计算簇心选择权重，让洞壁、小洞室和软沙附近更容易长植物。
        private static float ClusterCenterWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            return CellPlacementWeight(map, data, cell) + (NearSmallRoom(data, cell) ? 2.5f : 0f);
        }

        //函数职责：计算单格植物生成权重，让植物偏向洞穴边缘和软沙区域。
        private static float CellPlacementWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 1f;
            if (DesertPitGenUtility.NearCaveEdge(map, cell, 4))
            {
                weight += 3.5f;
            }

            if (cell.GetTerrain(map) == TerrainDefOf.SoftSand)
            {
                weight += 1.2f;
            }

            if (NearSmallRoom(data, cell))
            {
                weight += 2f;
            }

            return weight;
        }

        //函数职责：判断指定格子是否靠近记录的小洞室中心。
        private static bool NearSmallRoom(DesertPitLayoutData data, IntVec3 cell)
        {
            for (int i = 0; i < data.SmallRooms.Count; i++)
            {
                if (cell.DistanceTo(data.SmallRooms[i]) <= 11f)
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：判断植物是否需要使用较大的生成间隔。
        private static bool IsLargePlant(ThingDef plantDef)
        {
            return plantDef.defName.StartsWith("NingshaRace_DesertPitPlant") || plantDef == ThingDefOf.Agarilux;
        }

    }
}
