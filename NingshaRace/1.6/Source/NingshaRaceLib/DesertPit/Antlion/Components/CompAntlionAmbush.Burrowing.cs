using NingshaRaceLib.DesertPit.Antlion.AI;
using NingshaRaceLib.DesertPit.Antlion.Holders;
using NingshaRaceLib.DesertPit.Antlion.State;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Components
{
    //类职责：协调有限沙地搜索、步行返回和可中断下潜，不在无沙地时瞬移或隐藏身体。
    public sealed partial class CompAntlionAmbush
    {
        //函数职责：验证寻找沙地任务仍属于当前阶段与目的地。
        public bool CanContinueReturn(IntVec3 destination)
        {
            return AbleToAct && phase == AntlionPhase.SeekingSand && destination == sandDestination
                && AntlionSandUtility.CanBurrowAt(Pawn.Map, destination, Props, Pawn);
        }

        //函数职责：到达沙地后开始收拢身体，直到动画结束前仍保留地表 Pawn。
        public void NotifyReachedSand()
        {
            if (!CanContinueReturn(Pawn.Position)) return;
            phase = AntlionPhase.Submerging;
            phaseStartTick = Find.TickManager.TicksGame;
            Pawn.pather.StopDead();
        }

        //函数职责：路径失败后清除目的地并延迟再次搜索，避免同一格子反复生成失败任务。
        public void NotifySandPathFailed()
        {
            sandDestination = IntVec3.Invalid;
            nextSandSearchTick = Find.TickManager.TicksGame + Props.sandRetryTicks;
        }

        //函数职责：结束追击和加速，保留锚点以限制回程遇敌时的活动范围。
        private void BeginSeekingSand(int now)
        {
            phase = AntlionPhase.SeekingSand;
            phaseStartTick = now;
            target = null;
            targetReachable = false;
            unreachableSince = -1;
            pathFailurePosition = IntVec3.Invalid;
            burstUntilTick = 0;
            sandDestination = IntVec3.Invalid;
            nextSandSearchTick = now;
            if (!anchor.IsValid) anchor = Pawn.Position;
            StatDefOf.MoveSpeed.Worker.ClearCacheForThing(Pawn);
            StopOwnJob();
        }

        //函数职责：回程和下潜时允许近处猎物中断，否则按期限查找最近合法沙地。
        private void UpdateReturn(int now)
        {
            Pawn prey = InsideLeash(Pawn.Position)
                ? AntlionTargetUtility.FindNearest(Pawn.Map, Pawn.Position, Props, anchor, abandonedTarget) : null;
            if (prey != null)
            {
                target = prey;
                targetReachable = true;
                phase = AntlionPhase.Hunting;
                sandDestination = IntVec3.Invalid;
                StopOwnJob();
                return;
            }
            if (phase == AntlionPhase.Submerging) return;
            if (sandDestination.IsValid && !AntlionSandUtility.CanBurrowAt(Pawn.Map, sandDestination, Props, Pawn))
            {
                NotifySandPathFailed();
                StopOwnJob();
            }
            if (sandDestination.IsValid)
            {
                if (Pawn.Position == sandDestination)
                {
                    NotifyReachedSand();
                    StopOwnJob();
                }
                return;
            }
            if (now < nextSandSearchTick) return;
            nextSandSearchTick = now + Props.sandRetryTicks;
            sandDestination = AntlionSandUtility.FindNearest(Pawn, Props);
            if (sandDestination.IsValid) StopOwnJob();
        }

        //函数职责：完成前再次检查安全与近处猎物，再把原 Pawn 转入唯一地下容器。
        private void FinishSubmerging(int now)
        {
            UpdateReturn(now);
            if (phase != AntlionPhase.Submerging) return;
            if (!AbleToAct || !AntlionSandUtility.CanBurrowAt(Pawn.Map, Pawn.Position, Props, Pawn))
            {
                BeginSeekingSand(now);
                nextSandSearchTick = now + Props.sandRetryTicks;
                return;
            }
            AntlionBurrow.Store(Pawn, Pawn.Map, Pawn.Position, now + Props.rearmTicks);
        }
    }
}
