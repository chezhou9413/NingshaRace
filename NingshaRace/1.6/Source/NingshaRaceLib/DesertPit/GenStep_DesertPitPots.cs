using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit
{
    //类职责：在沙漠巨坑洞穴中散布可击碎的古旧砂陶罐遗迹建筑。
    public class GenStep_DesertPitPots : GenStep
    {
        //字段职责：记录当前生成步骤使用的稳定随机种子片段。
        private const int Seed = 914027342;

        //字段职责：限制入口周围罐子生成，避免干扰出入口区域。
        private const float MainSafeRadius = 10f;

        //字段职责：控制罐子之间的最小间隔。
        private const float MinSpacing = 2f;

        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：收集洞穴候选格并按地貌权重生成一批可破坏罐子。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("遗迹罐子");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            ThingDef glowDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlow");
            ThingDef potDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitPot");
            List<IntVec3> candidates = CollectCandidates(map, data, glowDef);
            if (candidates.Count == 0)
            {
                return;
            }

            List<IntVec3> placed = new List<IntVec3>();
            int targetCount = Mathf.Min(Rand.RangeInclusive(14, 24), candidates.Count);
            int guard = 0;
            while (placed.Count < targetCount && candidates.Count > 0 && guard < targetCount * 10)
            {
                IntVec3 cell;
                if (TryFindPotCell(map, data, candidates, placed, out cell))
                {
                    GenSpawn.Spawn(potDef, cell, map);
                    placed.Add(cell);
                    candidates.Remove(cell);
                }

                guard++;
            }
        }

        //函数职责：收集所有可放置罐子的基础候选格。
        private static List<IntVec3> CollectCandidates(Map map, DesertPitLayoutData data, ThingDef glowDef)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanUseCell(map, data, glowDef, cell))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        //函数职责：从候选池中按权重抽取一个满足间距的罐子位置。
        private static bool TryFindPotCell(Map map, DesertPitLayoutData data, List<IntVec3> candidates, List<IntVec3> placed, out IntVec3 cell)
        {
            List<IntVec3> localCandidates = new List<IntVec3>();
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 candidate = candidates[i];
                if (SpacingAllows(placed, candidate))
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

        //函数职责：判断单格是否满足基础占用、洞穴、安全区和地貌条件。
        private static bool CanUseCell(Map map, DesertPitLayoutData data, ThingDef glowDef, IntVec3 cell)
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

        //函数职责：判断候选格与已放置罐子之间是否保留足够间隔。
        private static bool SpacingAllows(List<IntVec3> placed, IntVec3 cell)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].DistanceTo(cell) < MinSpacing)
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：计算罐子放置权重，使其偏向小洞室、洞壁边缘、塌方附近和软沙。
        private static float CellPlacementWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 0.5f;
            if (NearSmallRoom(data, cell))
            {
                weight += 4f;
            }

            if (DesertPitGenUtility.NearCaveEdge(map, cell, 4))
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

            return Mathf.Max(weight, 0.1f);
        }

        //函数职责：判断指定格子是否靠近记录的小洞室中心。
        private static bool NearSmallRoom(DesertPitLayoutData data, IntVec3 cell)
        {
            for (int i = 0; i < data.SmallRooms.Count; i++)
            {
                if (cell.DistanceTo(data.SmallRooms[i]) <= 13f)
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
                if (cell.DistanceTo(data.Collapses[i]) <= 10f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
