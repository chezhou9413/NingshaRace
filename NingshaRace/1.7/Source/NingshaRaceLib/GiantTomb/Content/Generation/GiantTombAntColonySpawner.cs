using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.AntColony.State;
using NingshaRaceLib.GiantTomb.Content.Config;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Generation
{
    //类职责：在墓葬房间空地中生成无初始贵重物资的三乘三完整洞穴蚁群。
    internal static class GiantTombAntColonySpawner
    {
        //函数职责：按威胁条目的规模生成蚁穴、储藏格、初始成员和独立敌对阵营，并登记休眠成员。
        public static void Spawn(Map map, GiantTombContentCellPool cells, NingshaGiantTombThreatSpawn spawn, ref int colonyIndex, List<Pawn> spawnedPawns)
        {
            DefModExtension_AntColony settings = spawn.antNestDef.GetModExtension<DefModExtension_AntColony>();
            if (settings == null)
            {
                throw new InvalidOperationException(spawn.antNestDef.defName + ": 缺少DefModExtension_AntColony。");
            }
            AntColonyPopulationSettings population = AntColonyPopulationSettings.Create(settings, spawn.scale);
            for (int instance = 0; instance < spawn.count; instance++)
            {
                IntVec3 center = FindNestCenter(map, spawn.antNestDef, cells, population.StorageCellCount);
                List<IntVec3> buildArea = new List<IntVec3>(cells.Available);
                CellRect occupied = GenAdj.OccupiedRect(center, Rot4.North, spawn.antNestDef.size);
                cells.Reserve(occupied);
                List<IntVec3> storageCells = TakeStorageCells(cells, center, population.StorageCellCount);

                AntColonyState state = DesertPit.AntColony.Generation.DesertPitAntSceneUtility.SpawnColony(
                    map, spawn.antNestDef, center, storageCells, population, 1, 1, false, false, buildArea);
                colonyIndex++;
                state.PassageAnchor = cells.PassageAnchor();
                foreach (var building in state.Buildings) cells.Reserve(building.OccupiedRect());
                spawnedPawns.Add(state.Queen);
                spawnedPawns.AddRange(state.Members.Where(pawn => pawn.Spawned));
            }
        }

        //函数职责：随机选择一个完整三乘三占地均可用的蚁穴中心。
        private static IntVec3 FindNestCenter(Map map, ThingDef nestDef, GiantTombContentCellPool cells, int storageCount)
        {
            List<IntVec3> candidates = new List<IntVec3>(cells.Available);
            candidates.Shuffle();
            for (int i = 0; i < candidates.Count; i++)
            {
                CellRect occupied = GenAdj.OccupiedRect(candidates[i], Rot4.North, nestDef.size);
                List<IntVec3> storage = cells.Available.Where(cell => !occupied.Contains(cell))
                    .OrderBy(cell => cell.DistanceToSquared(candidates[i])).Take(storageCount).ToList();
                if (cells.ContainsAll(occupied) && storage.Count == storageCount
                    && map.GetComponent<MapComponent_DesertPitAntColonies>().TryPlanInitialBuildings(candidates[i], nestDef,
                        storage, 1, new List<IntVec3>(cells.Available), out _))
                {
                    return candidates[i];
                }
            }
            throw new InvalidOperationException("墓葬模板没有可容纳" + nestDef.size.x + "×" + nestDef.size.z + "蚁穴的连续空地: " + cells.TemplateDefName);
        }

        //函数职责：优先选择蚁穴周围的空格作为实体储藏位并立即预留。
        private static List<IntVec3> TakeStorageCells(GiantTombContentCellPool cells, IntVec3 center, int count)
        {
            List<IntVec3> ordered = cells.Available.OrderBy(cell => cell.DistanceToSquared(center)).ToList();
            if (ordered.Count < count)
            {
                throw new InvalidOperationException("墓葬蚁穴附近没有足够储藏格: " + cells.TemplateDefName + ", 需要" + count + "格。");
            }
            List<IntVec3> result = ordered.GetRange(0, count);
            for (int i = 0; i < result.Count; i++)
            {
                cells.ReserveItemStorage(result[i]);
            }
            return result;
        }

    }
}
