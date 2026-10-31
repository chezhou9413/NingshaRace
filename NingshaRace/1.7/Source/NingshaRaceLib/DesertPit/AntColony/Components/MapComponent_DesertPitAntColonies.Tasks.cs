using System;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //任务列表独占目标、建材与营养预留，执行结束统一释放。
    public partial class MapComponent_DesertPitAntColonies
    {
        //取得正在执行的唯一任务。
        public AntColonyTask TaskFor(Pawn pawn) => TryGetColony(pawn, out AntColonyState state)
            ? state.Tasks.Find(task => task.Worker == pawn) : null;

        //无论成功、中断或死亡，均从同一任务记录释放预留。
        public void ReleaseTask(Pawn pawn)
        {
            if (TryGetColony(pawn, out AntColonyState state)) ReleaseTaskInState(state, pawn);
        }
        private static void ReleaseTaskInState(AntColonyState state, Pawn pawn) => state.Tasks.RemoveAll(task => task.Worker == pawn);
        public void ReleaseForageAssignments(Pawn pawn) => ReleaseTask(pawn);

        //清理完成的那份任务，避免旧工作结束时移除刚领取的新工作。
        public void ReleaseTask(Pawn pawn, AntColonyTask task)
        {
            if (TryGetColony(pawn, out AntColonyState state)) state.Tasks.Remove(task);
        }

        //通道检测确定障碍后，破障也登记到统一列表并独占目标。
        public bool RegisterMiningTask(Pawn pawn, Thing blocker)
        {
            if (!TryGetColony(pawn, out AntColonyState state)
                || IsFullAlarm(state, Find.TickManager.TicksGame) || IsRetreating(state, Find.TickManager.TicksGame)
                || state.Tasks.Any(task => task.Target == blocker && task.Worker != pawn)) return false;
            ReleaseTask(pawn);
            state.Tasks.Add(new AntColonyTask { Kind = AntTaskKind.Mine, Target = blocker, Worker = pawn, Cell = blocker.Position });
            return true;
        }

        //只有已经分配给执行者的任务才占用营养。
        private static float ReservedNutrition(AntColonyState state, AntColonyTask except = null) =>
            state.Tasks.Where(task => task != except && task.Worker != null).Sum(task => task.Nutrition);

        //检查当前工作尚由该成员持有，受击和警报后不能继续扣料。
        public bool TaskIsValid(Pawn pawn)
        {
            AntColonyTask task = TaskFor(pawn);
            if (task == null || !TryGetColony(pawn, out AntColonyState state)) return false;
            if (IsFullAlarm(state, Find.TickManager.TicksGame) || IsRetreating(state, Find.TickManager.TicksGame)) return false;
            if (task.Target != null && task.Target.Destroyed && pawn.carryTracker.CarriedThing == null) return false;
            if (task.Destination != null && !task.Destination.Spawned) return false;
            return true;
        }

        //从已规划列表领取最高优先级的可执行工作；缺料任务不阻塞后续任务。
        private Job ClaimTask(Pawn pawn, AntColonyState state)
        {
            AntCaste caste = pawn.GetComp<Comp_DesertPitAntMember>().Caste;
            foreach (AntColonyTask task in state.Tasks.OrderBy(task => task.Priority).ToArray())
            {
                if (task.Worker != null || (task.Kind == AntTaskKind.Hatch) != (caste == AntCaste.Queen)) continue;
                if (caste != AntCaste.Queen && caste != AntCaste.Worker) continue;
                if (task.PreferredWorker != null && !task.PreferredWorker.Dead && task.PreferredWorker != pawn) continue;
                if (task.Kind == AntTaskKind.Dig && task.Target != pawn) continue;
                if (task.Kind == AntTaskKind.Haul && !task.FreeHaul
                    && !pawn.GetComp<Comp_DesertPitAntMember>().CanStartForage(Find.TickManager.TicksGame, Settings.workerHaulLimit)) continue;
                if (task.Nutrition > 0 && GetStoredNutrition(state) - GetFoodReserve(state)
                    - ReservedNutrition(state) <= task.Nutrition) continue;
                LocalTargetInfo target = task.Target?.Spawned == true ? new LocalTargetInfo(task.Target)
                    : task.Target?.ParentHolder is Building_AntColonyStructure source ? new LocalTargetInfo(source)
                    : new LocalTargetInfo(task.Cell);
                if (!pawn.CanReserveAndReach(target, task.Kind == AntTaskKind.Dig ? PathEndMode.OnCell : PathEndMode.Touch, Danger.Deadly)) continue;
                if (task.Kind == AntTaskKind.Build && !ReserveStone(state, task)) continue;
                task.Worker = pawn;
                Job job;
                if (task.Kind == AntTaskKind.Haul)
                {
                    job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_DesertPitAntHaul, target,
                        task.Destination != null ? new LocalTargetInfo(task.Destination) : new LocalTargetInfo(task.Cell));
                    job.count = Math.Min(task.Count, pawn.carryTracker.MaxStackSpaceEver(task.Target.def));
                }
                else job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_AntWork, target);
                return job;
            }
            return null;
        }

        //完成施工或孵化时再次验证实物与空间，然后一次结算。
        public bool CompleteTask(Pawn pawn)
        {
            AntColonyTask task = TaskFor(pawn);
            if (!TaskIsValid(pawn) || !TryGetColony(pawn, out AntColonyState state)) return false;
            int now = Find.TickManager.TicksGame;
            if (task.Kind == AntTaskKind.Hatch)
            {
                if (now < state.NextBirthTick || !TryGetMissingRegularCaste(state, out PawnKindDef missing, out _)
                    || missing != task.HatchKind || !TryFindMemberSpawnCell(state.NestPosition, out IntVec3 cell)
                    || !ConsumeStoredNutrition(state, pawn, task.Nutrition, task)) return false;
                SpawnMemberAt(state, task.HatchKind, cell);
                state.NextBirthTick = now + Settings.reproductionCooldownTicks;
            }
            else if (task.Kind == AntTaskKind.Build)
            {
                if (!CanBuildAt(state, task.BuildingDef, task.Cell, task) || !PayStone(state, task)) return false;
                SpawnStructure(state, task.BuildingDef, task.Cell);
            }
            else if (task.Kind == AntTaskKind.Repair)
            {
                if (task.Target == null || !task.Target.Spawned || !RepairAllowed(state, task.Target, now)
                    || !ConsumeStoredNutrition(state, GetNutritionConsumer(state), task.Nutrition, task)) return false;
                task.Target.HitPoints = Math.Min(task.Target.MaxHitPoints, task.Target.HitPoints + task.Count);
                if (task.Target == state.Nest) state.NextRepairTick = now + Settings.repairIntervalTicks;
            }
            else if (task.Kind == AntTaskKind.Upgrade)
            {
                if (!state.LevelingEnabled || state.CurrentLevel >= state.MaxLevel || now < state.NextUpgradeTick
                    || !ConsumeStoredNutrition(state, GetNutritionConsumer(state), task.Nutrition, task)) return false;
                IncreaseColonyLevel(state, now);
            }
            else if (task.Kind == AntTaskKind.Dig) CompleteResourceDig(pawn);
            state.Tasks.Remove(task);
            return true;
        }

        //搬运成功时更新账目，免费接续搬运不消耗五次额度。
        public void NotifyForageCompleted(Pawn pawn)
        {
            AntColonyTask task = TaskFor(pawn);
            if (task == null || !TryGetColony(pawn, out AntColonyState state)) return;
            if (!task.FreeHaul) pawn.GetComp<Comp_DesertPitAntMember>().NotifySuccessfulHaul(
                Find.TickManager.TicksGame, Settings.workerHaulLimit, Settings.workerHaulCooldownTicks);
            state.NutritionDirty = true;
            state.Tasks.Remove(task);
        }
    }
}
