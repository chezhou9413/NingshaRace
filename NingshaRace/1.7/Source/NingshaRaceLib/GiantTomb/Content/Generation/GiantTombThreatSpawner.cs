using System;
using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Erosion.Utility;
using NingshaRaceLib.GiantTomb.Content.Config;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Generation
{
    //类职责：抽取房间敌人结果并生成普通敌人、侵蚀体或完整洞穴蚁群。
    internal static class GiantTombThreatSpawner
    {
        //函数职责：抽取并执行一个房间模板配置的唯一敌人结果。
        public static void Spawn(Map map, GiantTombContentCellPool cells, NingshaGiantTombRoomProfile profile, ref int colonyIndex)
        {
            NingshaGiantTombEnemyOutcome outcome = GiantTombWeightedUtility.Pick(profile.enemyOutcomes, item => item.weight);
            List<Pawn> spawnedPawns = new List<Pawn>();
            for (int i = 0; i < outcome.spawns.Count; i++)
            {
                NingshaGiantTombThreatSpawn spawn = outcome.spawns[i];
                if (spawn.IsAntColony)
                {
                    GiantTombAntColonySpawner.Spawn(map, cells, spawn, ref colonyIndex, spawnedPawns);
                }
                else
                {
                    SpawnPawns(map, cells, spawn, spawnedPawns);
                }
            }
            GiantTombDormancyUtility.PutToSleep(map, spawnedPawns);
        }

        //函数职责：按XML数量生成同种Pawn、应用可选侵蚀体与永久精神状态并登记为房间休眠威胁。
        private static void SpawnPawns(Map map, GiantTombContentCellPool cells, NingshaGiantTombThreatSpawn spawn, List<Pawn> spawnedPawns)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(spawn.factionDef);
            if (faction == null)
            {
                throw new InvalidOperationException("墓葬敌人阵营不存在: " + spawn.factionDef.defName);
            }
            for (int i = 0; i < spawn.count; i++)
            {
                Pawn pawn;
                if (spawn.mutantDef == null)
                {
                    pawn = PawnGenerator.GeneratePawn(spawn.pawnKind, faction);
                }
                else
                {
                    if (spawn.mutantDef != DefOfRefs.NingshaRace_ErosionBodyMutant)
                    {
                        throw new InvalidOperationException("墓葬只支持生成凝砂侵蚀体: " + spawn.mutantDef.defName);
                    }
                    pawn = ErosionBodySpawnUtility.Generate(spawn.pawnKind, faction, map.Tile);
                }
                IntVec3 cell = cells.TakeRandom("敌人 " + spawn.pawnKind.defName);
                GenSpawn.Spawn(pawn, cell, map, Rot4.Random);
                if (spawn.permanentMentalState != null)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(spawn.permanentMentalState, null, forced: true);
                }
                spawnedPawns.Add(pawn);
            }
        }
    }
}
