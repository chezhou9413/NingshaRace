using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.AI;
using NingshaRaceLib.DesertPit.Antlion.State;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Antlion.Components
{
    //类职责：限制追猎范围、不可达超时和目标接替，并为思考树提供原版近战派生任务。
    public sealed partial class CompAntlionAmbush
    {
        //函数职责：使攻击任务逐帧确认目标和下一步仍在边界内，不补刀、不越界绕路。
        public bool CanContinueAttack(Pawn prey)
        {
            if (!AbleToAct || phase != AntlionPhase.Hunting || prey != target || !targetReachable
                || !AntlionTargetUtility.IsPrey(prey, Pawn.Map) || !InsideLeash(prey.Position)
                || !InsideLeash(Pawn.Position)) return false;
            return !Pawn.pather.Moving || InsideLeash(Pawn.pather.nextCell);
        }

        //函数职责：由路径失败任务记录连续受阻的起点，防止连通性缓存误判后无限重发任务。
        public void NotifyPathFailed()
        {
            if (unreachableSince < 0) unreachableSince = Find.TickManager.TicksGame;
            pathFailurePosition = Pawn.Position;
            targetReachable = false;
        }

        //函数职责：根据阶段构建有期限的伏击专用任务，等待期间不调用原版自动攻击。
        public Job CreateJob()
        {
            if (!AbleToAct) return null;
            Job job;
            if (phase == AntlionPhase.Hunting && CanContinueAttack(target))
            {
                job = JobMaker.MakeJob(DefOfRefs.NingshaRace_AntlionAttack, target);
                job.killIncappedTarget = false;
                job.attackDoorIfTargetLost = false;
            }
            else if (phase == AntlionPhase.SeekingSand && sandDestination.IsValid && Pawn.Position != sandDestination)
                job = JobMaker.MakeJob(DefOfRefs.NingshaRace_AntlionSeekSand, sandDestination);
            else job = JobMaker.MakeJob(DefOfRefs.NingshaRace_AntlionWait);
            job.expiryInterval = 60;
            job.checkOverrideOnExpire = true;
            job.canBashDoors = false;
            job.canBashFences = false;
            return job;
        }

        //函数职责：检查目标去留与持续不可达时间，只在失去目标时尝试一次近距离接替。
        private void UpdateHunt(int now)
        {
            if (!AntlionTargetUtility.IsPrey(target, Pawn.Map) || !InsideLeash(Pawn.Position)
                || !InsideLeash(target.Position)
                || (Pawn.pather.Moving && !InsideLeash(Pawn.pather.nextCell)))
            {
                ReplaceTargetOrReturn(now);
                return;
            }
            bool reachable = AntlionTargetUtility.CanReach(Pawn.Map, Pawn.Position, target, PathEndMode.Touch);
            bool stalled = pathFailurePosition == Pawn.Position
                && !Pawn.Position.AdjacentTo8WayOrInside(target.Position);
            if (reachable && !stalled)
            {
                unreachableSince = -1;
                pathFailurePosition = IntVec3.Invalid;
            }
            else if (unreachableSince < 0) unreachableSince = now;
            targetReachable = reachable && !stalled;
            if (!targetReachable) StopOwnJob();
            if (unreachableSince >= 0 && now - unreachableSince >= Props.unreachableTicks)
                ReplaceTargetOrReturn(now);
        }

        //函数职责：在原出土边界内寻找另一只近处猎物，不刷新爆发移速期限。
        private void ReplaceTargetOrReturn(int now)
        {
            //当前出土周期不立即重新选中刚放弃的猎物，避免受阻目标令追击与返回反复切换。
            abandonedTarget = target;
            Pawn replacement = InsideLeash(Pawn.Position)
                ? AntlionTargetUtility.FindNearest(Pawn.Map, Pawn.Position, Props, anchor, target) : null;
            if (replacement == null)
            {
                BeginSeekingSand(now);
                return;
            }
            target = replacement;
            targetReachable = true;
            unreachableSince = -1;
            pathFailurePosition = IntVec3.Invalid;
            StopOwnJob();
        }

        //函数职责：统一检查位置与最初出土点的距离。
        private bool InsideLeash(IntVec3 cell)
        {
            return AntlionTargetUtility.WithinLeash(cell, anchor, Props.chaseRadius);
        }
    }
}
