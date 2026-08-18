using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：集中实现蚁后补员、爆浆蚁波次和新成员实体生成。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：把指定巢群的存活爆浆蚁补充到独立数量上限。
        private void SpawnBoomAntsToCap(AntColonyState state)
        {
            int missing = state.Population.BoomAntCap - CountCaste(state, AntCaste.Boom);
            for (int i = 0; i < missing; i++)
            {
                if (SpawnMember(state, DefOfRefs.NingshaRace_DesertPitBoomAntKind) == null)
                {
                    break;
                }
            }
        }

        //函数职责：确认蚁后当前存在缺员、拥有足够实体食物，并选出本次取食时展示的物资。
        private bool CanStartReproduction(Pawn queen, AntColonyState state, out Thing feedingSource)
        {
            feedingSource = null;
            PawnKindDef kind;
            float nutritionCost;
            if (!TryGetMissingRegularCaste(state, out kind, out nutritionCost) || GetStoredNutrition(state, queen) < nutritionCost)
            {
                return false;
            }

            feedingSource = FindStoredFoodFor(queen, state);
            return feedingSource != null;
        }

        //函数职责：在蚁后完成取食和孵化后重新验证资源，优先消耗任务展示的食物并生成一只缺失成员。
        public bool CompleteReproduction(Pawn queen, Thing feedingSource)
        {
            AntColonyState state;
            if (!TryGetColony(queen, out state) || state.Queen != queen || queen.Dead || state.NestDestroyed || state.Nest == null || state.Nest.Destroyed)
            {
                return false;
            }

            int ticks = Find.TickManager.TicksGame;
            if (ticks < state.NextBirthTick)
            {
                return false;
            }

            PawnKindDef kind;
            float nutritionCost;
            if (!TryGetMissingRegularCaste(state, out kind, out nutritionCost))
            {
                return false;
            }

            IntVec3 spawnCell;
            if (!TryFindMemberSpawnCell(state.NestPosition, out spawnCell) || !ConsumeStoredNutrition(state, queen, nutritionCost, feedingSource))
            {
                return false;
            }

            Pawn newborn = SpawnMemberAt(state, kind, spawnCell);
            if (newborn == null)
            {
                return false;
            }

            state.NextBirthTick = ticks + Settings.reproductionCooldownTicks;
            return true;
        }

        //函数职责：按工蚁优先、兵蚁次之的顺序确定当前需要补充的常规阶级和营养消耗。
        private bool TryGetMissingRegularCaste(AntColonyState state, out PawnKindDef kind, out float nutritionCost)
        {
            int regularCount = state.Members.Count - CountCaste(state, AntCaste.Boom);
            if (regularCount >= state.Population.RegularAntCap)
            {
                kind = null;
                nutritionCost = 0f;
                return false;
            }

            if (CountCaste(state, AntCaste.Worker) < state.Population.WorkerTarget)
            {
                kind = DefOfRefs.NingshaRace_DesertPitWorkerAntKind;
                nutritionCost = Settings.workerNutritionCost;
                return true;
            }

            if (CountCaste(state, AntCaste.Soldier) < state.Population.SoldierTarget)
            {
                kind = DefOfRefs.NingshaRace_DesertPitSoldierAntKind;
                nutritionCost = Settings.soldierNutritionCost;
                return true;
            }

            kind = null;
            nutritionCost = 0f;
            return false;
        }

        //函数职责：创建指定种类的成年虫族 Pawn，写入巢群编号并生成到蚁穴附近。
        private Pawn SpawnMember(AntColonyState state, PawnKindDef kind)
        {
            IntVec3 spawnCell;
            if (!TryFindMemberSpawnCell(state.NestPosition, out spawnCell))
            {
                return null;
            }

            return SpawnMemberAt(state, kind, spawnCell);
        }

        //函数职责：在已经验证的格子创建成年虫族 Pawn，并把它登记为指定巢群成员。
        private Pawn SpawnMemberAt(AntColonyState state, PawnKindDef kind, IntVec3 spawnCell)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(kind, state.Faction);
            Comp_DesertPitAntMember memberComp = pawn.TryGetComp<Comp_DesertPitAntMember>();
            memberComp.AssignColony(state.Id);
            GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
            FleckMaker.ThrowDustPuff(pawn.DrawPos, map, 1.2f);
            if (!state.Members.Contains(pawn))
            {
                state.Members.Add(pawn);
            }

            coloniesByPawn[pawn] = state;
            return pawn;
        }

        //函数职责：在蚁穴附近寻找可站立、无建筑且能够生成新成员的格子。
        private bool TryFindMemberSpawnCell(IntVec3 center, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomCellNear(
                center,
                map,
                6,
                delegate(IntVec3 candidate)
                {
                    return candidate.InBounds(map) && candidate.Standable(map) && candidate.GetEdifice(map) == null && candidate.GetFirstPawn(map) == null;
                },
                out cell,
                120);
        }
    }
}
