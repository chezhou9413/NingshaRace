using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.AltarMissions.Map
{
    //类职责：在解救任务中央生成凝砂族待救者，并在四角轮流生成三倍侵蚀体。
    public sealed class GenStep_AltarRescueTargets : GenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 812740151;

        //函数职责：建立隐藏友方阵营、生成一至三名待救者和三倍待机侵蚀体并登记目标。
        public override void Generate(Verse.Map map, GenStepParams parms)
        {
            Faction rescueFaction = GetOrCreateRescueFaction();
            List<Pawn> rescuees = new List<Pawn>();
            List<Pawn> erosionBodies = new List<Pawn>();
            int rescueCount = Rand.RangeInclusive(1, 3);
            IntVec3 center = map.Center;
            for (int i = 0; i < rescueCount; i++)
            {
                IntVec3 cell = FindStandableNear(map, center, 9);
                Pawn pawn = PawnGenerator.GeneratePawn(DefOfRefs.NingshaRace_Colonist, rescueFaction);
                GenSpawn.Spawn(pawn, cell, map, Rot4.Random);
                rescuees.Add(pawn);
            }
            IntVec3[] corners =
            {
                new IntVec3(12, 0, 12), new IntVec3(map.Size.x - 13, 0, 12),
                new IntVec3(map.Size.x - 13, 0, map.Size.z - 13), new IntVec3(12, 0, map.Size.z - 13)
            };
            for (int i = 0; i < rescueCount * 3; i++)
            {
                IntVec3 cell = FindCornerCell(map, corners[i % corners.Length]);
                Pawn pawn = ErosionBodySpawnUtility.Spawn(map, cell, DefOfRefs.NingshaRace_Colonist, Faction.OfEntities);
                pawn.mindState.duty = new PawnDuty(DutyDefOf.IdleNoInteraction, cell, 4f);
                erosionBodies.Add(pawn);
            }
            map.GetComponent<MissionMapComponent>().InitializeRescueTargets(rescuees, erosionBodies);
        }

        //函数职责：取得隐藏救援阵营，不存在时创建并设为玩家盟友。
        private static Faction GetOrCreateRescueFaction()
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(DefOfRefs.NingshaRace_RescueFaction);
            if (faction == null)
            {
                faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(DefOfRefs.NingshaRace_RescueFaction));
                Find.FactionManager.Add(faction);
            }
            faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Ally, false);
            return faction;
        }

        //函数职责：在中央指定半径内找到可站立且未被Pawn占用的生成格。
        private static IntVec3 FindStandableNear(Verse.Map map, IntVec3 center, int radius)
        {
            if (CellFinder.TryFindRandomCellNear(center, map, radius,
                cell => cell.Standable(map) && cell.GetFirstPawn(map) == null, out IntVec3 result, 300))
            {
                return result;
            }
            throw new InvalidOperationException("解救任务中央没有可生成待救者的位置。");
        }

        //函数职责：在对应角落寻找距玩家入场点至少二十格的侵蚀体生成格。
        private static IntVec3 FindCornerCell(Verse.Map map, IntVec3 corner)
        {
            IntVec3 playerStart = MapGenerator.PlayerStartSpot;
            if (CellFinder.TryFindRandomCellNear(corner, map, 10,
                cell => cell.Standable(map) && cell.GetFirstPawn(map) == null
                    && (!playerStart.IsValid || cell.DistanceTo(playerStart) >= 20f), out IntVec3 result, 300))
            {
                return result;
            }
            throw new InvalidOperationException("解救任务角落没有距玩家入场区二十格以上的侵蚀体位置。");
        }
    }
}
