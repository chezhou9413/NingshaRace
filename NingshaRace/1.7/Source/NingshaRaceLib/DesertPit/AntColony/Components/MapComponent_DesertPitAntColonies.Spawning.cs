using RimWorld;
using Verse;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //普通成员经孵化任务出生，爆浆蚁使用独立补员时钟。
    public partial class MapComponent_DesertPitAntColonies
    {
        //休眠爆浆蚁仍在 Members 中计数，只有真正消耗后才补充。
        private void SpawnBoomAntsToCap(AntColonyState state)
        {
            if (ColonyDormant(state)) return;
            int missing = state.Population.BoomAntCap - CountCaste(state, AntCaste.Boom);
            for (int i = 0; i < missing; i++)
            {
                if (!TryFindMemberSpawnCell(state.NestPosition, out IntVec3 cell)) break;
                Pawn pawn = SpawnMemberAt(state, DefOfRefs.NingshaRace_DesertPitBoomAntKind, cell);
                if (!IsFullAlarm(state, Find.TickManager.TicksGame) && state.Intruders.Count == 0) state.Nest.Enter(pawn);
            }
        }

        //按工蚁、飞镰蚁、兵蚁、破军蚁、吐酸蚁确定单次任务的固定种类和费用。
        private bool TryGetMissingRegularCaste(AntColonyState state, out PawnKindDef kind, out float nutritionCost)
        {
            if (CountCaste(state, AntCaste.Worker) < state.Population.WorkerTarget)
            { kind = DefOfRefs.NingshaRace_DesertPitWorkerAntKind; nutritionCost = Settings.workerNutritionCost; return true; }
            if (CountCaste(state, AntCaste.Flying) < state.Population.FlyingTarget)
            { kind = DefOfRefs.NingshaRace_DesertPitFlyingAntKind; nutritionCost = Settings.flyingNutritionCost; return true; }
            if (CountCaste(state, AntCaste.Soldier) < state.Population.SoldierTarget)
            { kind = DefOfRefs.NingshaRace_DesertPitSoldierAntKind; nutritionCost = Settings.soldierNutritionCost; return true; }
            if (CountCaste(state, AntCaste.Breaker) < state.Population.BreakerTarget)
            { kind = DefOfRefs.NingshaRace_DesertPitBreakerAntKind; nutritionCost = Settings.breakerNutritionCost; return true; }
            if (CountCaste(state, AntCaste.Acid) < state.Population.AcidTarget)
            { kind = DefOfRefs.NingshaRace_DesertPitAcidAntKind; nutritionCost = Settings.acidNutritionCost; return true; }
            kind = null;
            nutritionCost = 0;
            return false;
        }

        //在已验证的格子生成原版 Pawn，组件事件完成成员登记。
        private Pawn SpawnMemberAt(AntColonyState state, PawnKindDef kind, IntVec3 cell)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(kind, state.Faction);
            pawn.GetComp<Comp_DesertPitAntMember>().AssignColony(state.Id);
            GenSpawn.Spawn(pawn, cell, map, Rot4.Random);
            FleckMaker.ThrowDustPuff(pawn.DrawPos, map, 1.2f);
            return pawn;
        }

        //补员不覆盖建筑或其他 Pawn。
        private bool TryFindMemberSpawnCell(IntVec3 center, out IntVec3 cell) => CellFinder.TryFindRandomCellNear(center, map, 6,
            candidate => candidate.InBounds(map) && candidate.Standable(map) && candidate.GetEdifice(map) == null
                && candidate.GetFirstPawn(map) == null, out cell, 120);
    }
}
