using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：根据巢群状态为不同阶级蚂蚁创建防御、进食、繁殖、搬运和巡逻工作。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：按阶级和当前警戒状态为一只蚂蚁创建最高优先级工作。
        public Job TryCreateColonyJob(Pawn pawn)
        {
            AntColonyState state;
            if (!TryGetColony(pawn, out state))
            {
                return null;
            }

            Comp_DesertPitAntMember memberComp = pawn.TryGetComp<Comp_DesertPitAntMember>();
            AntCaste caste = memberComp.Caste;
            if (IsRetreating(state, Find.TickManager.TicksGame))
            {
                return TryCreateRetreatJob(pawn, state, caste);
            }

            Thing intruder = FindNearestIntruder(pawn, state);

            if (caste == AntCaste.Boom)
            {
                return intruder != null ? CreateBoomAttackJob(intruder) : CreatePatrolJob(pawn, state, Settings.workerWanderRadius);
            }

            if (state.Frenzy)
            {
                return intruder != null ? CreateMeleeAttackJob(intruder) : CreatePatrolJob(pawn, state, Settings.soldierPatrolRadius);
            }

            if (caste == AntCaste.Soldier && intruder != null)
            {
                return CreateMeleeAttackJob(intruder);
            }

            if ((caste == AntCaste.Worker || caste == AntCaste.Queen) && intruder != null && pawn.Position.DistanceTo(intruder.Position) <= Settings.workerRetreatRadius)
            {
                return CreateReturnToNestJob(pawn, state);
            }

            Job needsJob = TryCreateNeedsJob(pawn, state);
            if (needsJob != null)
            {
                return needsJob;
            }

            if (caste == AntCaste.Queen)
            {
                Thing reproductionFood;
                if (!state.NestDestroyed && state.Nest != null && !state.Nest.Destroyed && Find.TickManager.TicksGame >= state.NextBirthTick && CanStartReproduction(pawn, state, out reproductionFood))
                {
                    Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_DesertPitAntReproduce, state.Nest, reproductionFood);
                    job.count = Settings.reproductionWorkTicks;
                    return job;
                }

                if (pawn.Position.DistanceTo(state.NestPosition) > Settings.queenLeashRadius)
                {
                    return CreateReturnToNestJob(pawn, state);
                }

                return CreatePatrolJob(pawn, state, Settings.queenLeashRadius);
            }

            if (caste == AntCaste.Worker)
            {
                Job forageJob = TryCreateForageJob(pawn, state);
                return forageJob ?? CreatePatrolJob(pawn, state, Settings.workerWanderRadius);
            }

            return CreatePatrolJob(pawn, state, Settings.soldierPatrolRadius);
        }

        //函数职责：在巢群实体储藏格中寻找可食用物资，或让过度疲劳的成员在巢穴附近休息。
        private Job TryCreateNeedsJob(Pawn pawn, AntColonyState state)
        {
            if (pawn.needs != null && pawn.needs.food != null && pawn.needs.food.CurLevelPercentage < 0.35f)
            {
                Thing food = FindStoredFoodFor(pawn, state);
                if (food != null)
                {
                    Job ingestJob = JobMaker.MakeJob(JobDefOf.Ingest, food);
                    ingestJob.count = food is Corpse ? 1 : System.Math.Min(food.stackCount, FoodUtility.WillIngestStackCountOf(pawn, food.def, FoodUtility.NutritionForEater(pawn, food)));
                    return ingestJob;
                }
            }

            if (pawn.needs != null && pawn.needs.rest != null && pawn.needs.rest.CurLevelPercentage < 0.2f)
            {
                IntVec3 restCell;
                if (TryFindColonyCell(pawn, state, Settings.queenLeashRadius, out restCell))
                {
                    return JobMaker.MakeJob(JobDefOf.LayDown, restCell);
                }
            }

            return null;
        }

        //函数职责：从本巢实体储藏格中寻找当前成员可以预留并抵达的新鲜食物。
        private Thing FindStoredFoodFor(Pawn pawn, AntColonyState state)
        {
            Thing result = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < state.StorageCells.Count; i++)
            {
                Thing food = GetStorageOccupant(state.StorageCells[i]);
                if (!IsStoredFood(food) || !food.IngestibleNow || !pawn.CanReserveAndReach(food, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    continue;
                }

                float distance = pawn.Position.DistanceToSquared(food.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    result = food;
                }
            }

            return result;
        }

        //函数职责：创建带有有限攻击次数和失去目标后破门行为的近战拦截工作。
        private static Job CreateMeleeAttackJob(Thing target)
        {
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.maxNumMeleeAttacks = 1;
            job.expiryInterval = 500;
            job.attackDoorIfTargetLost = true;
            return job;
        }

        //函数职责：创建爆浆蚁追踪目标并在接近后自爆的专用工作。
        private static Job CreateBoomAttackJob(Thing target)
        {
            Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_DesertPitBoomAntDetonate, target);
            job.expiryInterval = 500;
            return job;
        }

        //函数职责：判断爆浆蚁与当前目标的距离是否已经达到配置的自爆阈值。
        public bool IsBoomInTriggerRange(Pawn boomAnt, Thing target)
        {
            return boomAnt != null && target != null && boomAnt.Position.DistanceTo(target.Position) <= Settings.boomTriggerDistance;
        }

        //函数职责：创建成员返回蚁穴附近安全格的移动工作。
        private Job CreateReturnToNestJob(Pawn pawn, AntColonyState state)
        {
            IntVec3 cell;
            if (!TryFindColonyCell(pawn, state, Settings.queenLeashRadius, out cell))
            {
                return JobMaker.MakeJob(JobDefOf.Wait, 120);
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            job.expiryInterval = 500;
            return job;
        }

        //函数职责：在指定半径内选择一个可达格，让成员形成围绕蚁穴的自然巡逻和游荡。
        private Job CreatePatrolJob(Pawn pawn, AntColonyState state, float radius)
        {
            IntVec3 cell;
            if (!TryFindColonyCell(pawn, state, radius, out cell) || cell == pawn.Position)
            {
                return JobMaker.MakeJob(JobDefOf.Wait, 120);
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
            job.locomotionUrgency = LocomotionUrgency.Walk;
            job.expiryInterval = 500;
            return job;
        }

        //函数职责：在蚁穴周围寻找地图内、可站立且成员能够抵达的活动格。
        private bool TryFindColonyCell(Pawn pawn, AntColonyState state, float radius, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomCellNear(
                state.NestPosition,
                map,
                UnityEngine.Mathf.CeilToInt(radius),
                delegate(IntVec3 candidate)
                {
                    return candidate.InBounds(map) && candidate.DistanceTo(state.NestPosition) <= radius && candidate.Standable(map) && pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Deadly);
                },
                out cell,
                80);
        }
    }
}
