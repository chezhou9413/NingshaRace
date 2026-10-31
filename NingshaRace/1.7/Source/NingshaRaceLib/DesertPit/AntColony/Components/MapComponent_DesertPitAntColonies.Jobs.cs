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
                return TryCreateOrphanJob(pawn);
            }

            Comp_DesertPitAntMember memberComp = pawn.TryGetComp<Comp_DesertPitAntMember>();
            AntCaste caste = memberComp.Caste;
            if (caste == AntCaste.Queen && IsFullAlarm(state, Find.TickManager.TicksGame))
                return JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_AntEnterNest, state.Nest);
            //工蚁放弃危险区域任务后先回巢，避免留在死亡热点附近继续游荡。
            if (caste == AntCaste.Worker && IsWorkerForageDangerous(pawn, pawn.Position)
                && pawn.Position.DistanceTo(state.NestPosition) > Settings.queenLeashRadius)
                return CreateReturnToNestJob(pawn, state);
            //极度饥饿时仍保留进食机会，避免警报永久遮蔽基本需求。
            if (caste != AntCaste.Boom && pawn.needs?.food?.CurLevelPercentage < 0.12f)
            {
                Job urgentFood = TryCreateNeedsJob(pawn, state);
                if (urgentFood != null) return urgentFood;
            }
            if (IsRetreating(state, Find.TickManager.TicksGame))
            {
                return TryCreateRetreatJob(pawn, state, caste);
            }

            Thing intruder = caste == AntCaste.Worker || caste == AntCaste.Queen
                ? FindNearbyWorkerThreat(pawn, state) : FindNearestIntruder(pawn, state);

            if (caste == AntCaste.Boom)
            {
                return intruder != null ? CreateBoomAttackJob(intruder)
                    : !IsFullAlarm(state, Find.TickManager.TicksGame)
                        ? JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_AntEnterNest, state.Nest)
                        : CreatePatrolJob(pawn, state, 7f);
            }

            if (IsCombatCaste(caste) && intruder != null)
            {
                return CreateCombatJob(pawn, intruder) ?? CreateReturnToNestJob(pawn, state);
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
            if (IsFullAlarm(state, Find.TickManager.TicksGame)) return CreateGuardJob(pawn, state);

            if (caste == AntCaste.Queen)
            {
                Job hatch = ClaimTask(pawn, state);
                if (hatch != null) return hatch;

                if (pawn.Position.DistanceTo(state.NestPosition) > Settings.queenLeashRadius)
                {
                    return CreateReturnToNestJob(pawn, state);
                }

                return CreatePatrolJob(pawn, state, Settings.queenLeashRadius);
            }

            if (caste == AntCaste.Worker)
            {
                return ClaimTask(pawn, state) ?? CreatePatrolJob(pawn, state, Settings.workerWanderRadius);
            }

            return CreatePatrolJob(pawn, state, Settings.soldierPatrolRadius);
        }

        //函数职责：在巢群实体储藏格中寻找可食用物资，或让过度疲劳的成员在巢穴附近休息。
        private Job TryCreateNeedsJob(Pawn pawn, AntColonyState state)
        {
            if (pawn.needs != null && pawn.needs.food != null && pawn.needs.food.CurLevelPercentage < 0.3f)
            {
                Thing food = FindStoredFoodFor(pawn, state) ?? FindLooseFoodFor(pawn);
                if (food != null)
                {
                    if (food.ParentHolder is Buildings.Building_AntColonyStructure container)
                        return JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_AntTakeFood, container, food);
                    Job ingestJob = JobMaker.MakeJob(JobDefOf.Ingest, food);
                    ingestJob.count = food is Corpse ? 1 : System.Math.Min(food.stackCount, FoodUtility.WillIngestStackCountOf(pawn, food.def, FoodUtility.NutritionForEater(pawn, food)));
                    return ingestJob;
                }
            }

            if (pawn.needs != null && pawn.needs.rest != null && pawn.needs.rest.CurLevelPercentage < 0.2f)
            {
                IntVec3 restCell;
                if (TryFindColonyCell(pawn, state, 7f, out restCell, forRest: true))
                {
                    return JobMaker.MakeJob(JobDefOf.LayDown, restCell);
                }
            }

            return null;
        }

        //函数职责：从本巢实体储藏格中寻找当前成员可以预留并抵达的新鲜食物。
        private Thing FindStoredFoodFor(Pawn pawn, AntColonyState state)
        {
            foreach (Thing food in StoredFoods(state))
            {
                Thing access = food.Spawned ? food : food.ParentHolder as Thing;
                if (!IsStoredFood(food) || !food.IngestibleNow || !pawn.RaceProps.CanEverEat(food)
                    || access == null || !pawn.CanReserveAndReach(access, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    continue;
                }

                return food;
            }

            return null;
        }

        //函数职责：创建带有有限攻击次数和失去目标后破门行为的近战拦截工作。
        private static Job CreateMeleeAttackJob(Thing target)
        {
            Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_DesertPitAntMelee, target);
            //第二目标标记巢群战斗，破障任务仍独立检查近战能力。
            job.targetB = target;
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

        //引爆前再次确认目标仍在允许防御的区域，且进入实际自爆距离。
        public bool IsBoomInTriggerRange(Pawn boomAnt, Thing target)
        {
            return boomAnt != null && boomAnt.Spawned && target != null && target.Spawned
                && target.Map == boomAnt.Map && !target.Destroyed
                && !(target is Pawn victim && (victim.Dead || victim.Downed))
                && CanContinueColonyAttack(boomAnt, target)
                && boomAnt.Position.DistanceTo(target.Position) <= Settings.boomTriggerDistance;
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
        private bool TryFindColonyCell(Pawn pawn, AntColonyState state, float radius, out IntVec3 cell, bool forRest = false)
        {
            return CellFinder.TryFindRandomCellNear(
                state.NestPosition,
                map,
                UnityEngine.Mathf.CeilToInt(radius),
                delegate(IntVec3 candidate)
                {
                    return candidate.InBounds(map) && candidate.DistanceTo(state.NestPosition) <= radius
                        && !IsWorkerForageDangerous(pawn, candidate)
                        && (!forRest || (pawn.CanReserve(candidate) && candidate.GetFirstPawn(map) == null))
                        && candidate.Standable(map) && pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Deadly);
                },
                out cell,
                80);
        }
    }
}
