using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.GiantTomb.Discovery.Generation
{
    //类职责：在每张新沙漠巨坑中保证放置一个远离入口且不会被后续场景覆盖的破损石棺。
    public sealed class GenStep_GiantTombCoffinEntrance : GenStep
    {
        //类职责：保存一个已经通过占地与邻接检查的石棺位置及朝向。
        private sealed class PlacementCandidate
        {
            //字段职责：记录石棺多格占地使用的锚点。
            public IntVec3 Cell;

            //字段职责：记录石棺贴图与占地使用的旋转方向。
            public Rot4 Rotation;

            //字段职责：记录生成石棺前需要清除的松散物品与植物数量。
            public int ClutterCount;
        }

        //字段职责：为破损石棺位置选择提供稳定随机种子片段。
        private const int Seed = 147193157;

        //字段职责：要求破损石棺远离沙漠巨坑主入口。
        private const float MinimumEntranceDistance = 24f;

        //字段职责：在远端洞室被岩屑占满时仍保持与主入口的最低安全距离。
        private const float FallbackEntranceDistance = 18f;

        //字段职责：判定入口或主洞室取得的洞穴连通区是否足以容纳场景选址。
        private const int MinimumUsableCaveRegionCells = 64;

        //字段职责：规定石棺及调查位置需要避让后续场景的半径。
        private const float ReserveRadius = 5f;

        //字段职责：列出石棺允许使用的四个朝向。
        private static readonly Rot4[] Rotations = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };

        //属性职责：向地图生成器提供当前步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：收集全部有效位置、按远离入口和靠近遗迹地貌的程度选择并生成唯一石棺。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("隐藏墓葬入口");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            ThingDef coffinDef = DefOfRefs.NingshaRace_GiantTombBrokenCoffin;
            IntVec3 reachabilityRoot = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : data.MainCenter;
            HashSet<IntVec3> reachableCaves = GiantTombCoffinCaveRegionUtility.CollectReachable(map, reachabilityRoot);
            if (reachableCaves.Count < MinimumUsableCaveRegionCells)
            {
                HashSet<IntVec3> mainCaves = GiantTombCoffinCaveRegionUtility.CollectReachable(map, data.MainCenter);
                if (mainCaves.Count > reachableCaves.Count)
                {
                    reachableCaves = mainCaves;
                }
            }

            if (reachableCaves.Count < MinimumUsableCaveRegionCells)
            {
                HashSet<IntVec3> largestCaves = GiantTombCoffinCaveRegionUtility.CollectLargest(map);
                if (largestCaves.Count > reachableCaves.Count)
                {
                    reachableCaves = largestCaves;
                }
            }

            List<PlacementCandidate> candidates = CollectCandidates(map, data, coffinDef, reachableCaves, MinimumEntranceDistance, false);
            if (candidates.Count == 0)
            {
                candidates = CollectCandidates(map, data, coffinDef, reachableCaves, MinimumEntranceDistance, true);
            }

            if (candidates.Count == 0)
            {
                candidates = CollectCandidates(map, data, coffinDef, reachableCaves, FallbackEntranceDistance, true);
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("沙漠巨坑无法放置破损砂岩石棺入口。可达洞穴格：" + reachableCaves.Count + "，场景保留格：" + data.ReservedSceneCells.Count + "。");
            }

            PlacementCandidate selected = SelectCandidate(data, candidates);
            ClearPlacementClutter(map, selected.Cell, selected.Rotation, coffinDef.Size);
            GenSpawn.Spawn(ThingMaker.MakeThing(coffinDef), selected.Cell, map, selected.Rotation);
            ReserveArea(map, data, selected.Cell, selected.Rotation, coffinDef.Size);
        }

        //函数职责：枚举洞穴格和四向旋转并保存占地、邻接与安全检查全部通过的候选。
        private static List<PlacementCandidate> CollectCandidates(Map map, DesertPitLayoutData data, ThingDef coffinDef, HashSet<IntVec3> reachableCaves, float minimumDistance, bool allowClutter)
        {
            List<PlacementCandidate> result = new List<PlacementCandidate>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.DistanceTo(data.MainCenter) < minimumDistance)
                {
                    continue;
                }

                for (int i = 0; i < Rotations.Length; i++)
                {
                    Rot4 rotation = Rotations[i];
                    int clutterCount;
                    if (CanPlaceAt(map, data, cell, rotation, coffinDef.Size, reachableCaves, allowClutter, out clutterCount))
                    {
                        result.Add(new PlacementCandidate { Cell = cell, Rotation = rotation, ClutterCount = clutterCount });
                    }
                }
            }

            return result;
        }

        //函数职责：验证石棺完整占地为空，并保证至少存在一个可以接触石棺的相邻洞穴格。
        private static bool CanPlaceAt(Map map, DesertPitLayoutData data, IntVec3 anchor, Rot4 rotation, IntVec2 size, HashSet<IntVec3> reachableCaves, bool allowClutter, out int clutterCount)
        {
            clutterCount = 0;
            CellRect occupied = GenAdj.OccupiedRect(anchor, rotation, size);
            foreach (IntVec3 cell in occupied)
            {
                int cellClutter;
                if (!reachableCaves.Contains(cell) || !IsSafeCaveCell(map, data, cell, allowClutter, out cellClutter))
                {
                    return false;
                }

                clutterCount += cellClutter;
            }

            foreach (IntVec3 cell in occupied)
            {
                for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
                {
                    IntVec3 adjacent = cell + GenAdj.CardinalDirections[i];
                    int ignoredClutter;
                    if (!occupied.Contains(adjacent) && reachableCaves.Contains(adjacent) && IsSafeCaveCell(map, data, adjacent, true, out ignoredClutter))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        //函数职责：判断格子是否属于干燥、可站立且没有不可清除实体占用的天然洞穴地面。
        private static bool IsSafeCaveCell(Map map, DesertPitLayoutData data, IntVec3 cell, bool allowClutter, out int clutterCount)
        {
            clutterCount = 0;
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map))
            {
                return false;
            }

            if (data.ProtectedRouteCells.Contains(cell) || data.ReservedSceneCells.Contains(cell) || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)))
            {
                return false;
            }

            if (cell.GetEdifice(map) != null || cell.GetPlant(map) != null || cell.GetFirstPawn(map) != null)
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.def.category == ThingCategory.Building || thing is Pawn)
                {
                    return false;
                }

                if (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Plant)
                {
                    if (!allowClutter || !thing.def.destroyable)
                    {
                        return false;
                    }

                    clutterCount++;
                }
            }

            return true;
        }

        //函数职责：优先选择需要清理杂物最少的位置，再按远离入口和靠近遗迹地貌的程度加权。
        private static PlacementCandidate SelectCandidate(DesertPitLayoutData data, List<PlacementCandidate> candidates)
        {
            int minimumClutter = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                minimumClutter = Mathf.Min(minimumClutter, candidates[i].ClutterCount);
            }

            List<PlacementCandidate> finalists = candidates.FindAll(candidate => candidate.ClutterCount == minimumClutter);
            return finalists.RandomElementByWeight(candidate => PlacementWeight(data, candidate.Cell));
        }

        //函数职责：仅清除最终占地中的可破坏松散物品和植物，为石棺留下完整空置地面。
        private static void ClearPlacementClutter(Map map, IntVec3 anchor, Rot4 rotation, IntVec2 size)
        {
            foreach (IntVec3 cell in GenAdj.OccupiedRect(anchor, rotation, size))
            {
                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (thing.def.destroyable && (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Plant))
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        //函数职责：提高远离主入口、靠近小洞室或塌方的候选被选中概率。
        private static float PlacementWeight(DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = Mathf.Max(1f, cell.DistanceTo(data.MainCenter) - MinimumEntranceDistance + 1f);
            for (int i = 0; i < data.SmallRooms.Count; i++)
            {
                if (cell.DistanceTo(data.SmallRooms[i]) <= 12f)
                {
                    weight += 18f;
                    break;
                }
            }

            for (int i = 0; i < data.Collapses.Count; i++)
            {
                if (cell.DistanceTo(data.Collapses[i]) <= 10f)
                {
                    weight += 12f;
                    break;
                }
            }

            return weight;
        }

        //函数职责：把石棺周围空间登记为后续蚁巢、生态、遗迹和植物共同避让的保留区。
        private static void ReserveArea(Map map, DesertPitLayoutData data, IntVec3 anchor, Rot4 rotation, IntVec2 size)
        {
            IntVec3 center = GenAdj.OccupiedRect(anchor, rotation, size).CenterCell;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ReserveRadius, true))
            {
                if (cell.InBounds(map))
                {
                    data.ReservedSceneCells.Add(cell);
                }
            }
        }
    }
}
