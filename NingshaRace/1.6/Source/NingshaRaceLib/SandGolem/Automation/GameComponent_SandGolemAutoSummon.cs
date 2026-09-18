using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.SandGolem.Automation
{
    //类职责：维护自动召唤开关，接收沙傀消失通知，并把单次施法任务排在当前工作之后。
    public sealed class GameComponent_SandGolemAutoSummon : GameComponent
    {
        private List<SandGolemAutoSummonState> states = new List<SandGolemAutoSummonState>();

        //构造职责：供游戏创建独立的自动召唤调度组件。
        public GameComponent_SandGolemAutoSummon(Game game) { }

        //属性职责：返回当前游戏的自动召唤调度器。
        public static GameComponent_SandGolemAutoSummon Current => Verse.Current.Game?.GetComponent<GameComponent_SandGolemAutoSummon>();

        //属性职责：取得仅用于自动召唤的任务定义。
        public static JobDef AutoJob => DefDatabase<JobDef>.GetNamed("NingshaRace_AutoSummonSandGolem");

        //函数职责：保存开启自动召唤的角色和未完成请求。
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "sandGolemAutoSummon", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && states == null) states = new List<SandGolemAutoSummonState>();
        }

        //函数职责：判断指定角色的自动召唤开关是否开启。
        public bool Enabled(Pawn pawn) => StateFor(pawn) != null;

        //函数职责：查找角色对应的唯一自动召唤状态。
        private SandGolemAutoSummonState StateFor(Pawn pawn) => states.Find(state => state.caster == pawn);

        //函数职责：切换开关，只有未征召且居住区有可达沙地的玩家凝砂族可以开启。
        public void Toggle(Pawn pawn)
        {
            if (Enabled(pawn)) { Disable(pawn); return; }
            if (!SandGolemUtility.IsPlayerNingshaPawn(pawn) || !pawn.Spawned || pawn.Dead || pawn.Drafted)
            {
                Messages.Message("未征召的凝砂族才能开启沙傀自动召唤。", pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!SandGolemAutoSummonTarget.TryFind(pawn, false, out IntVec3 cell))
            {
                NotifyNoSand(pawn);
                return;
            }
            states.Add(new SandGolemAutoSummonState { caster = pawn, homeSand = cell,
                pending = GameComponent_SandGolemTracker.Current.GolemForCaster(pawn) == null });
            Schedule(StateFor(pawn));
        }

        //函数职责：关闭开关并撤销自动任务，不操作手动施法和其他排队工作。
        public void Disable(Pawn pawn)
        {
            states.RemoveAll(state => state.caster == pawn);
            ClearQueued(pawn);
            if (pawn?.CurJobDef == AutoJob) pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        //函数职责：把自然消失或死亡合并为一个补召请求，等待旧沙傀完全移除。
        public void NotifyLost(Pawn pawn)
        {
            SandGolemAutoSummonState state = StateFor(pawn);
            if (state != null) state.pending = true;
        }

        //函数职责：手动召回、手动替换或成功生成时撤销过时的自动补召请求。
        public void ClearRequest(Pawn pawn)
        {
            SandGolemAutoSummonState state = StateFor(pawn);
            if (state != null) state.pending = false;
            ClearQueued(pawn);
        }

        //函数职责：移除队列中本模块的任务，并释放相应预订。
        private static void ClearQueued(Pawn pawn)
        {
            JobQueue queue = pawn?.jobs?.jobQueue;
            if (queue == null) return;
            for (int i = queue.Count - 1; i >= 0; i--)
                if (queue[i].job.def == AutoJob) queue.Extract(queue[i].job).Cleanup(pawn, true);
        }

        //函数职责：低频检查沙地与角色状态，暂停倒地、精神状态或离开地图的角色。
        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0) return;
            for (int i = states.Count - 1; i >= 0; i--)
            {
                SandGolemAutoSummonState state = states[i];
                Pawn pawn = state.caster;
                if (pawn == null || pawn.Destroyed || pawn.Dead || !SandGolemUtility.IsPlayerNingshaPawn(pawn) || pawn.Drafted)
                {
                    Disable(pawn);
                    continue;
                }
                if (!pawn.Spawned || pawn.Downed || pawn.InMentalState) continue;
                bool reachable = SandGolemAutoSummonTarget.IsHomeSand(pawn, state.homeSand, false)
                    && pawn.CanReach(state.homeSand, PathEndMode.Touch, Danger.Some);
                if (!reachable && !SandGolemAutoSummonTarget.TryFind(pawn, false, out state.homeSand))
                {
                    Disable(pawn);
                    NotifyNoSand(pawn);
                    continue;
                }
                Schedule(state);
            }
        }

        //函数职责：复用能力创建任务，只向当前任务之后插入一次，不中断正在执行的工作。
        private static void Schedule(SandGolemAutoSummonState state)
        {
            Pawn pawn = state.caster;
            if (!state.pending || !pawn.Spawned || pawn.Drafted || pawn.Downed || pawn.InMentalState
                || GameComponent_SandGolemTracker.Current.GolemForCaster(pawn) != null) return;
            Ability ability = pawn.abilities?.GetAbility(DefOfRefs.NingshaRace_Ability_SummonSandGolem);
            if (ability == null || !ability.CanQueueCast || ability.GizmoDisabled(out _)) return;
            if (!SandGolemAutoSummonTarget.TryFind(pawn, true, out IntVec3 cell)) return;
            Job job = ability.GetJob(cell, LocalTargetInfo.Invalid);
            job.def = AutoJob;
            pawn.jobs.jobQueue.EnqueueFirst(job, JobTag.Misc);
        }

        //函数职责：向右侧信件栏说明无可用居住区沙地导致自动召唤关闭。
        private static void NotifyNoSand(Pawn pawn)
        {
            Find.LetterStack.ReceiveLetter("沙傀自动召唤未开启",
                pawn.LabelShortCap + "的居住区内没有可到达且允许使用的沙地，沙傀自动召唤已关闭。", LetterDefOf.NeutralEvent, pawn);
        }
    }
}
