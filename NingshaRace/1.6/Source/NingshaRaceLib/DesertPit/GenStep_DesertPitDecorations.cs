using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit
{
    //类职责：在沙漠巨坑洞穴中按地貌权重散布钟乳石、骨骸和少量发光水晶装饰。
    public class GenStep_DesertPitDecorations : GenStep
    {
        //字段职责：记录当前生成步骤使用的稳定随机种子片段。
        private const int Seed = 914027337;

        //字段职责：限制入口周围装饰物生成，避免干扰出入口区域。
        private const float MainSafeRadius = 8f;

        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：收集洞穴候选格并按高密度洞壁簇群、小洞室遗骸和稀有水晶生成装饰物。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("洞穴散饰");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            ThingDef glowDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlow");
            List<IntVec3> candidates = CollectCandidates(map, data, glowDef);
            if (candidates.Count == 0)
            {
                return;
            }

            List<IntVec3> placed = new List<IntVec3>();
            int targetCount = Mathf.Min(Rand.RangeInclusive(105, 155), candidates.Count);
            int clusterCount = Mathf.Min(Rand.RangeInclusive(22, 34), candidates.Count);
            int crystalTarget = Mathf.Min(Rand.RangeInclusive(1, 3), Mathf.Max(1, targetCount / 18));
            int crystalPlaced = 0;
            for (int i = 0; i < clusterCount && placed.Count < targetCount; i++)
            {
                IntVec3 center = candidates.RandomElementByWeight((IntVec3 cell) => ClusterCenterWeight(map, data, cell));
                ScatterCluster(map, data, candidates, placed, center, targetCount, crystalTarget, ref crystalPlaced);
            }

            FillRemainingDecorations(map, data, candidates, placed, targetCount, crystalTarget, ref crystalPlaced);
        }

        //函数职责：收集所有可放置洞穴装饰物的基础候选格。
        private static List<IntVec3> CollectCandidates(Map map, DesertPitLayoutData data, ThingDef glowDef)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanPlaceDecoration(map, data, glowDef, cell))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        //函数职责：在指定簇心附近生成三到八个适合当前地貌的装饰物。
        private static void ScatterCluster(Map map, DesertPitLayoutData data, List<IntVec3> candidates, List<IntVec3> placed, IntVec3 center, int targetCount, int crystalTarget, ref int crystalPlaced)
        {
            int count = Rand.RangeInclusive(3, 8);
            float radius = Rand.Range(4f, 8f);
            for (int i = 0; i < count && placed.Count < targetCount; i++)
            {
                ThingDef decorationDef = DesertPitDecorationUtility.ChooseDecorationDef(crystalPlaced < crystalTarget);
                IntVec3 cell;
                if (TryFindClusterCell(map, data, candidates, placed, center, radius, decorationDef, out cell))
                {
                    SpawnDecoration(map, decorationDef, cell);
                    placed.Add(cell);
                    candidates.Remove(cell);
                    if (DesertPitDecorationUtility.IsCrystal(decorationDef))
                    {
                        crystalPlaced++;
                    }
                }
            }
        }

        //函数职责：簇群未达到目标数量时补足少量零散装饰物。
        private static void FillRemainingDecorations(Map map, DesertPitLayoutData data, List<IntVec3> candidates, List<IntVec3> placed, int targetCount, int crystalTarget, ref int crystalPlaced)
        {
            int guard = 0;
            while (placed.Count < targetCount && candidates.Count > 0 && guard < 900)
            {
                ThingDef decorationDef = DesertPitDecorationUtility.ChooseDecorationDef(crystalPlaced < crystalTarget);
                IntVec3 cell;
                if (TryFindAnyCell(map, data, candidates, placed, decorationDef, out cell))
                {
                    SpawnDecoration(map, decorationDef, cell);
                    placed.Add(cell);
                    candidates.Remove(cell);
                    if (DesertPitDecorationUtility.IsCrystal(decorationDef))
                    {
                        crystalPlaced++;
                    }
                }
                else
                {
                    break;
                }

                guard++;
            }
        }

        //函数职责：从指定簇范围内选择一个符合当前装饰物间距和地貌偏好的格子。
        private static bool TryFindClusterCell(Map map, DesertPitLayoutData data, List<IntVec3> candidates, List<IntVec3> placed, IntVec3 center, float radius, ThingDef decorationDef, out IntVec3 cell)
        {
            List<IntVec3> localCandidates = new List<IntVec3>();
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 candidate = candidates[i];
                if (candidate.DistanceTo(center) <= radius && SpacingAllows(placed, candidate, decorationDef))
                {
                    localCandidates.Add(candidate);
                }
            }

            if (localCandidates.Count == 0)
            {
                cell = IntVec3.Invalid;
                return false;
            }

            cell = localCandidates.RandomElementByWeight((IntVec3 candidate) => CellPlacementWeight(map, data, candidate, decorationDef));
            return true;
        }

        //函数职责：从剩余候选格中选择一个符合当前装饰物间距和地貌偏好的格子。
        private static bool TryFindAnyCell(Map map, DesertPitLayoutData data, List<IntVec3> candidates, List<IntVec3> placed, ThingDef decorationDef, out IntVec3 cell)
        {
            List<IntVec3> localCandidates = new List<IntVec3>();
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 candidate = candidates[i];
                if (SpacingAllows(placed, candidate, decorationDef))
                {
                    localCandidates.Add(candidate);
                }
            }

            if (localCandidates.Count == 0)
            {
                cell = IntVec3.Invalid;
                return false;
            }

            cell = localCandidates.RandomElementByWeight((IntVec3 candidate) => CellPlacementWeight(map, data, candidate, decorationDef));
            return true;
        }

        //函数职责：判断格子是否满足基础占用、洞穴、安全区和地貌条件。
        private static bool CanPlaceDecoration(Map map, DesertPitLayoutData data, ThingDef glowDef, IntVec3 cell)
        {
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map))
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

        //函数职责：判断新装饰物与已放置装饰物之间是否保留足够间隔。
        private static bool SpacingAllows(List<IntVec3> placed, IntVec3 cell, ThingDef decorationDef)
        {
            float minDistance = 1f;
            if (DesertPitDecorationUtility.IsCrystal(decorationDef))
            {
                minDistance = 5f;
            }
            else if (DesertPitDecorationUtility.IsLargeDecoration(decorationDef))
            {
                minDistance = 1.45f;
            }

            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].DistanceTo(cell) < minDistance)
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：计算簇心选择权重，让装饰簇偏向洞壁、小洞室、塌方边缘和软沙。
        private static float ClusterCenterWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 1f;
            if (DesertPitGenUtility.NearCaveEdge(map, cell, 4))
            {
                weight += 4f;
            }

            if (NearSmallRoom(data, cell))
            {
                weight += 2.5f;
            }

            if (NearCollapse(data, cell))
            {
                weight += 2f;
            }

            if (cell.GetTerrain(map) == TerrainDefOf.SoftSand)
            {
                weight += 1.2f;
            }

            return weight;
        }

        //函数职责：按装饰物类型计算单格生成权重。
        private static float CellPlacementWeight(Map map, DesertPitLayoutData data, IntVec3 cell, ThingDef decorationDef)
        {
            float weight = 0.5f;
            bool nearEdge = DesertPitGenUtility.NearCaveEdge(map, cell, 4);
            bool nearSmallRoom = NearSmallRoom(data, cell);
            bool softSand = cell.GetTerrain(map) == TerrainDefOf.SoftSand;
            if (DesertPitDecorationUtility.IsStalactite(decorationDef))
            {
                weight += nearEdge ? 8f : 0.3f;
                weight += nearSmallRoom ? 2f : 0f;
                weight += NearCollapse(data, cell) ? 2.5f : 0f;
            }
            else if (DesertPitDecorationUtility.IsBones(decorationDef))
            {
                weight += nearSmallRoom ? 3.5f : 0f;
                weight += softSand ? 1.8f : 0f;
                weight += nearEdge ? 0.8f : 0f;
            }
            else if (DesertPitDecorationUtility.IsCrystal(decorationDef))
            {
                weight += nearEdge ? 4f : 0.5f;
                weight += nearSmallRoom ? 2f : 0f;
                weight += softSand ? 2f : 0f;
            }

            return Mathf.Max(weight, 0.1f);
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

        //函数职责：生成指定装饰物并放入地图。
        private static void SpawnDecoration(Map map, ThingDef decorationDef, IntVec3 cell)
        {
            GenSpawn.Spawn(decorationDef, cell, map);
        }
    }
}
