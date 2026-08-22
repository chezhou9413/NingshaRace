using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.AntColony.State;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.AntColony.Generation
{
    //类职责：选择自然洞室位置并生成一个完整蚁穴、实体储藏、初始成员和储备物资场景。
    public static class DesertPitAntSceneUtility
    {
        //字段职责：规定蚁巢场景与主入口之间的最小距离。
        private const float EntranceSafeRadius = 25f;

        //字段职责：规定两个独立蚁巢中心之间的最小距离。
        private const float ColonySpacing = 28f;

        //字段职责：规定场景生成和物品禁止标记使用的蚁巢保护半径。
        private const float SceneReserveRadius = 10f;

        //函数职责：寻找一个符合洞穴与间距规则的位置，并生成完整巢群场景。
        public static bool TryGenerateColony(Map map, DesertPitLayoutData data, List<IntVec3> existingCenters, out IntVec3 center)
        {
            ThingDef nestDef = DefOfRefs.NingshaRace_DesertPitAntNest;
            List<IntVec3> candidates = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanPlaceSceneAt(map, data, existingCenters, nestDef, cell))
                {
                    candidates.Add(cell);
                }
            }

            if (candidates.Count == 0)
            {
                center = IntVec3.Invalid;
                return false;
            }

            center = candidates.RandomElementByWeight(delegate(IntVec3 cell)
            {
                return ScenePlacementWeight(map, data, cell);
            });
            GenerateScene(map, data, nestDef, center, existingCenters.Count);
            return true;
        }

        //函数职责：验证蚁穴占地、储藏环、入口距离、巢群间距和受保护路线均满足要求。
        private static bool CanPlaceSceneAt(Map map, DesertPitLayoutData data, List<IntVec3> existingCenters, ThingDef nestDef, IntVec3 center)
        {
            if (center.DistanceTo(data.MainCenter) < EntranceSafeRadius || data.ProtectedRouteCells.Contains(center) || data.ReservedSceneCells.Contains(center))
            {
                return false;
            }

            for (int i = 0; i < existingCenters.Count; i++)
            {
                if (center.DistanceTo(existingCenters[i]) < ColonySpacing)
                {
                    return false;
                }
            }

            CellRect occupied = GenAdj.OccupiedRect(center, Rot4.North, nestDef.size);
            foreach (IntVec3 cell in occupied)
            {
                if (!IsClearDryCaveCell(map, data, cell))
                {
                    return false;
                }
            }

            List<IntVec3> storageCells = CollectStorageCells(map, data, occupied);
            return storageCells.Count >= nestDef.GetModExtension<DefModExtension_AntColony>().storageCellCount;
        }

        //函数职责：判断一个格子是否是未占用、干燥、可站立且不属于保留路线的洞穴地面。
        private static bool IsClearDryCaveCell(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map) || data.ProtectedRouteCells.Contains(cell) || data.ReservedSceneCells.Contains(cell))
            {
                return false;
            }

            if (DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)) || cell.GetEdifice(map) != null || cell.GetPlant(map) != null)
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
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

        //函数职责：从蚁穴外沿两格的环带中收集可用实体储藏格。
        private static List<IntVec3> CollectStorageCells(Map map, DesertPitLayoutData data, CellRect occupied)
        {
            List<IntVec3> cells = new List<IntVec3>();
            foreach (IntVec3 cell in occupied.ExpandedBy(2).EdgeCells)
            {
                if (IsClearDryCaveCell(map, data, cell))
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        //函数职责：计算场景候选权重，使蚁巢优先出现在小洞室和洞穴边缘附近。
        private static float ScenePlacementWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 1f;
            for (int i = 0; i < data.SmallRooms.Count; i++)
            {
                float distance = cell.DistanceTo(data.SmallRooms[i]);
                if (distance <= 8f)
                {
                    weight += 12f;
                }
                else if (distance <= 15f)
                {
                    weight += 5f;
                }
            }

            if (DesertPitGenUtility.NearCaveEdge(map, cell, 5))
            {
                weight += 2f;
            }

            return weight;
        }

        //函数职责：保留巢区、生成蚁穴与初始成员，并把完整状态登记到地图组件。
        private static void GenerateScene(Map map, DesertPitLayoutData data, ThingDef nestDef, IntVec3 center, int colonyIndex)
        {
            DefModExtension_AntColony settings = nestDef.GetModExtension<DefModExtension_AntColony>();
            int currentLevel = Rand.RangeInclusive(settings.initialLevelMin, settings.initialLevelMax);
            int maximumLevel = Rand.RangeInclusive(settings.maximumLevelMin, settings.maximumLevelMax);
            currentLevel = Mathf.Min(currentLevel, maximumLevel);
            AntColonyPopulationSettings population = AntColonyPopulationSettings.CreateForLevel(settings, currentLevel);
            MapComponent_DesertPitAntColonies manager = map.GetComponent<MapComponent_DesertPitAntColonies>();
            Faction faction = manager.GetColonyFaction(colonyIndex);
            CellRect occupied = GenAdj.OccupiedRect(center, Rot4.North, nestDef.size);
            List<IntVec3> storagePool = CollectStorageCells(map, data, occupied);
            storagePool.Shuffle();
            List<IntVec3> storageCells = storagePool.GetRange(0, population.StorageCellCount);
            ReserveSceneArea(map, data, center);

            Building_DesertPitAntNest nest = (Building_DesertPitAntNest)ThingMaker.MakeThing(nestDef);
            GenSpawn.Spawn(nest, center, map, Rot4.North);
            nest.SetFaction(faction);

            List<Pawn> members = new List<Pawn>();
            Pawn queen = SpawnInitialMember(map, center, DefOfRefs.NingshaRace_DesertPitQueenAntKind, faction);
            members.Add(queen);
            for (int i = 0; i < population.WorkerTarget; i++)
            {
                members.Add(SpawnInitialMember(map, center, DefOfRefs.NingshaRace_DesertPitWorkerAntKind, faction));
            }

            for (int i = 0; i < population.SoldierTarget; i++)
            {
                members.Add(SpawnInitialMember(map, center, DefOfRefs.NingshaRace_DesertPitSoldierAntKind, faction));
            }

            SpawnInitialStock(map, storageCells);
            manager.RegisterGeneratedColony(nest, queen, members, storageCells, faction, population, true, currentLevel, maximumLevel);
            ForbidSceneHaulables(map, center);
        }

        //函数职责：将蚁巢十格场景内全部可搬运物品标记为玩家禁止，包含初始物资与既有岩块。
        private static void ForbidSceneHaulables(Map map, IntVec3 center)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, SceneReserveRadius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing.def.EverHaulable)
                    {
                        thing.SetForbidden(true, false);
                    }
                }
            }
        }

        //函数职责：把巢穴周围场景半径登记为后续生成步骤不可占用的保留区域。
        private static void ReserveSceneArea(Map map, DesertPitLayoutData data, IntVec3 center)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, SceneReserveRadius, true))
            {
                if (cell.InBounds(map))
                {
                    data.ReservedSceneCells.Add(cell);
                }
            }
        }

        //函数职责：生成一只成年虫族成员并放在蚁穴附近的可站立格。
        private static Pawn SpawnInitialMember(Map map, IntVec3 center, PawnKindDef kind, Faction faction)
        {
            IntVec3 spawnCell;
            if (!CellFinder.TryFindRandomCellNear(center, map, 7, delegate(IntVec3 cell)
            {
                return cell.InBounds(map) && cell.Standable(map) && cell.GetEdifice(map) == null && cell.GetFirstPawn(map) == null;
            }, out spawnCell, 160))
            {
                throw new System.InvalidOperationException("沙漠巨坑蚁巢附近没有可生成蚂蚁的格子。");
            }

            Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
            GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
            return pawn;
        }

        //函数职责：在实体储藏格放置约三只蚂蚁所需食物和一至两堆随机贵重品。
        private static void SpawnInitialStock(Map map, List<IntVec3> storageCells)
        {
            Thing jelly = ThingMaker.MakeThing(ThingDefOf.InsectJelly);
            jelly.stackCount = Mathf.Min(75, jelly.def.stackLimit);
            GenSpawn.Spawn(jelly, storageCells[0], map);

            List<ThingDef> valuables = new List<ThingDef>
            {
                ThingDefOf.Silver,
                ThingDefOf.Gold,
                ThingDefOf.Jade,
                ThingDefOf.ComponentIndustrial,
                ThingDefOf.ComponentSpacer
            };
            int pileCount = Rand.RangeInclusive(1, 2);
            for (int i = 0; i < pileCount; i++)
            {
                ThingDef def = valuables.RandomElement();
                valuables.Remove(def);
                Thing thing = ThingMaker.MakeThing(def);
                thing.stackCount = InitialValuableCount(def);
                GenSpawn.Spawn(thing, storageCells[i + 1], map);
            }
        }

        //函数职责：按贵重品类型给出适合作为小型巢穴战利品的初始堆叠数量。
        private static int InitialValuableCount(ThingDef def)
        {
            if (def == ThingDefOf.Silver)
            {
                return Rand.RangeInclusive(50, 120);
            }

            if (def == ThingDefOf.ComponentIndustrial)
            {
                return Rand.RangeInclusive(2, 5);
            }

            if (def == ThingDefOf.ComponentSpacer)
            {
                return Rand.RangeInclusive(1, 2);
            }

            return Rand.RangeInclusive(10, 25);
        }
    }
}
