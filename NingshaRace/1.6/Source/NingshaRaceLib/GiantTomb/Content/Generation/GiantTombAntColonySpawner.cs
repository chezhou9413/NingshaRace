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
                IntVec3 center = FindNestCenter(spawn.antNestDef, cells);
                CellRect occupied = GenAdj.OccupiedRect(center, Rot4.North, spawn.antNestDef.size);
                cells.Reserve(occupied);
                List<IntVec3> storageCells = TakeStorageCells(cells, center, population.StorageCellCount);

                MapComponent_DesertPitAntColonies manager = map.GetComponent<MapComponent_DesertPitAntColonies>();
                Faction faction = manager.GetColonyFaction(colonyIndex++);
                Building_DesertPitAntNest nest = (Building_DesertPitAntNest)ThingMaker.MakeThing(spawn.antNestDef);
                GenSpawn.Spawn(nest, center, map, Rot4.North);
                nest.SetFaction(faction);

                List<Pawn> members = new List<Pawn>();
                Pawn queen = SpawnMember(map, cells, storageCells, center, DefOfRefs.NingshaRace_DesertPitQueenAntKind, faction);
                members.Add(queen);
                for (int i = 0; i < population.WorkerTarget; i++)
                {
                    members.Add(SpawnMember(map, cells, storageCells, center, DefOfRefs.NingshaRace_DesertPitWorkerAntKind, faction));
                }
                for (int i = 0; i < population.SoldierTarget; i++)
                {
                    members.Add(SpawnMember(map, cells, storageCells, center, DefOfRefs.NingshaRace_DesertPitSoldierAntKind, faction));
                }
                manager.RegisterGeneratedColony(nest, queen, members, storageCells, faction, population, false, 1, 1);
                spawnedPawns.AddRange(members);
            }
        }

        //函数职责：随机选择一个完整三乘三占地均可用的蚁穴中心。
        private static IntVec3 FindNestCenter(ThingDef nestDef, GiantTombContentCellPool cells)
        {
            List<IntVec3> candidates = new List<IntVec3>(cells.Available);
            candidates.Shuffle();
            for (int i = 0; i < candidates.Count; i++)
            {
                CellRect occupied = GenAdj.OccupiedRect(candidates[i], Rot4.North, nestDef.size);
                if (cells.ContainsAll(occupied))
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

        //函数职责：在蚁穴附近的普通空格或暂时空闲储藏格生成一只成年蚁群成员。
        private static Pawn SpawnMember(Map map, GiantTombContentCellPool cells, List<IntVec3> storageCells, IntVec3 center, PawnKindDef kind, Faction faction)
        {
            List<IntVec3> candidates = cells.Available
                .Where(candidate => candidate.DistanceToSquared(center) <= 64f && candidate.GetFirstPawn(map) == null)
                .Concat(storageCells.Where(candidate => candidate.DistanceToSquared(center) <= 64f && candidate.GetFirstPawn(map) == null))
                .ToList();
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("墓葬蚁穴附近没有可生成成员的格子: " + cells.TemplateDefName + ", 种类=" + kind.defName);
            }
            IntVec3 cell = candidates.RandomElement();
            if (cells.Available.Contains(cell))
            {
                cells.Reserve(cell);
            }
            Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
            GenSpawn.Spawn(pawn, cell, map, Rot4.Random);
            return pawn;
        }
    }
}
