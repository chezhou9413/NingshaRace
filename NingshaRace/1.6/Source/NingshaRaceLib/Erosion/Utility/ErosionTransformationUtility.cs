using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Erosion.Utility
{
    //类职责：集中处理侵蚀体转化期间的移动锁定、Hediff 和被携带状态。
    public static class ErosionTransformationUtility
    {
        //函数职责：停止当前路径与工作，并持续禁用寻路器以锁定转化中的 Pawn。
        public static void LockPawn(Pawn pawn, bool stopJobs)
        {
            if (pawn?.pather != null)
            {
                pawn.pather.StopDead();
                pawn.pather.debugDisabled = true;
            }
            if (stopJobs)
            {
                pawn?.jobs?.StopAll();
            }
        }

        //函数职责：解除转化阶段设置的寻路器禁用状态。
        public static void UnlockPawn(Pawn pawn)
        {
            if (pawn?.pather != null)
            {
                pawn.pather.debugDisabled = false;
            }
        }

        //函数职责：确保转化专用 Hediff 存在，以禁用移动、操作与交流能力。
        public static void EnsureTransformationHediff(Pawn pawn)
        {
            if (pawn != null && !pawn.health.hediffSet.HasHediff(DefOfRefs.NingshaRace_ErosionTransformation))
            {
                pawn.health.AddHediff(DefOfRefs.NingshaRace_ErosionTransformation);
            }
        }

        //函数职责：移除转化专用 Hediff，避免其影响已经完成突变的 Pawn。
        public static void RemoveTransformationHediff(Pawn pawn)
        {
            if (pawn != null
                && pawn.health.hediffSet.TryGetHediff(DefOfRefs.NingshaRace_ErosionTransformation, out Hediff hediff))
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        //函数职责：若转化者正被其他 Pawn 携带，则在转化完成前将其放回地图。
        public static void DropFromCarrier(Pawn pawn)
        {
            if (pawn?.ParentHolder is Pawn_CarryTracker carryTracker)
            {
                carryTracker.TryDropCarriedThing(
                    carryTracker.pawn.Position,
                    ThingPlaceMode.Near,
                    out Thing _);
                carryTracker.pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
