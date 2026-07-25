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
    //类职责：在沙漠巨坑洞穴中少量生成图腾，形成接近原版废墟遗留物的地图装饰建筑。
    public class GenStep_DesertPitRelics : GenStep
    {
        //字段职责：记录当前生成步骤使用的稳定随机种子片段。
        private const int Seed = 914027341;

        //字段职责：限制入口周围遗迹建筑生成，避免图腾干扰出入口区域。
        private const float MainSafeRadius = 12f;

        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：收集洞穴候选格并按小洞室、洞壁和塌方边缘权重散布遗迹建筑。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("古代遗迹");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            ThingDef glowDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlow");
            List<IntVec3> candidates = CollectCandidates(map, data, glowDef);
            if (candidates.Count == 0)
            {
                return;
            }

            List<IntVec3> placed = new List<IntVec3>();
            int targetCount = Mathf.Min(Rand.RangeInclusive(8, 14), candidates.Count);
            int guard = 0;
            while (placed.Count < targetCount && candidates.Count > 0 && guard < targetCount * 12)
            {
                ThingDef relicDef = DesertPitRelicUtility.ChooseRelicDef(false);
                Rot4 rotation = DesertPitRelicUtility.ChooseRotation(relicDef);
                IntVec3 cell;
                if (TryFindRelicCell(map, data, glowDef, candidates, placed, relicDef, rotation, out cell))
                {
                    GenSpawn.Spawn(relicDef, cell, map, rotation);
                    placed.Add(cell);
                    RemoveOccupiedCandidates(candidates, cell, rotation, relicDef);
                }

                guard++;
            }
        }

        //函数职责：收集所有可作为遗迹建筑中心点的基础候选格。
        private static List<IntVec3> CollectCandidates(Map map, DesertPitLayoutData data, ThingDef glowDef)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanUseBaseCell(map, data, glowDef, cell))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        //函数职责：从候选格中按权重选择一个满足占地、间距和洞穴条件的位置。
        private static bool TryFindRelicCell(Map map, DesertPitLayoutData data, ThingDef glowDef, List<IntVec3> candidates, List<IntVec3> placed, ThingDef relicDef, Rot4 rotation, out IntVec3 cell)
        {
            List<IntVec3> localCandidates = new List<IntVec3>();
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 candidate = candidates[i];
                if (CanPlaceRelicAt(map, data, glowDef, candidate, rotation, relicDef) && SpacingAllows(placed, candidate, relicDef))
                {
                    localCandidates.Add(candidate);
                }
            }

            if (localCandidates.Count == 0)
            {
                cell = IntVec3.Invalid;
                return false;
            }

            cell = localCandidates.RandomElementByWeight((IntVec3 candidate) => CellPlacementWeight(map, data, candidate, relicDef));
            return true;
        }

        //函数职责：判断遗迹中心点和完整占地区域是否都满足放置条件。
        private static bool CanPlaceRelicAt(Map map, DesertPitLayoutData data, ThingDef glowDef, IntVec3 center, Rot4 rotation, ThingDef relicDef)
        {
            foreach (IntVec3 occupiedCell in GenAdj.CellsOccupiedBy(center, rotation, relicDef.size))
            {
                if (!CanUseBaseCell(map, data, glowDef, occupiedCell))
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：判断单个格子是否满足基础占用、洞穴、安全区和地貌条件。
        private static bool CanUseBaseCell(Map map, DesertPitLayoutData data, ThingDef glowDef, IntVec3 cell)
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

        //函数职责：判断新遗迹与已放置遗迹之间是否保留足够间隔。
        private static bool SpacingAllows(List<IntVec3> placed, IntVec3 cell, ThingDef relicDef)
        {
            float minDistance = DesertPitRelicUtility.IsSarcophagus(relicDef) ? 7f : 4.5f;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].DistanceTo(cell) < minDistance)
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：计算遗迹建筑放置权重，使其偏向小洞室、洞壁凹陷和塌方边缘。
        private static float CellPlacementWeight(Map map, DesertPitLayoutData data, IntVec3 cell, ThingDef relicDef)
        {
            float weight = 0.4f;
            bool nearSmallRoom = NearSmallRoom(data, cell);
            if (nearSmallRoom)
            {
                weight += DesertPitRelicUtility.IsSarcophagus(relicDef) ? 8f : 4f;
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

        //函数职责：从候选池中移除已经被大型遗迹占用的格子，减少后续重复检查。
        private static void RemoveOccupiedCandidates(List<IntVec3> candidates, IntVec3 center, Rot4 rotation, ThingDef relicDef)
        {
            foreach (IntVec3 occupiedCell in GenAdj.CellsOccupiedBy(center, rotation, relicDef.size))
            {
                candidates.Remove(occupiedCell);
            }
        }
    }
}
