using NingshaRaceLib.SandGolem.Utility;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.SandGolem.AI
{
    //汇聚和消散期间占住任务入口，避免沙傀反复领取搬运工作。
    public sealed class JobGiver_SandGolemAnimationWait : ThinkNode_JobGiver
    {
        //用短等待任务保持动画锁定，动画结束后重新扫描工作。
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!SandGolemUtility.IsMovementLockedSandGolem(pawn)) return null;
            Job job = JobMaker.MakeJob(JobDefOf.Wait);
            job.expiryInterval = 30;
            job.checkOverrideOnExpire = true;
            return job;
        }
    }
}
