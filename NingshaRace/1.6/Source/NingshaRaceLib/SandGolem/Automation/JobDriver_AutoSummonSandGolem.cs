using System.Collections.Generic;
using NingshaRaceLib.SandGolem.Tracking;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.SandGolem.Automation
{
    //类职责：走到施法位置并执行原版能力流程，只对自动补召任务检查居住区限制。
    public sealed class JobDriver_AutoSummonSandGolem : JobDriver_CastAbility
    {
        //函数职责：预订本次召唤沙地，避免多名召唤者选择同一落点。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        //函数职责：在移动和施法期间持续校验开关、目标和已有沙傀，保留原版冷却与动画。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !GameComponent_SandGolemAutoSummon.Current.Enabled(pawn) || pawn.Drafted
                || pawn.Downed || pawn.InMentalState || !SandGolemAutoSummonTarget.IsHomeSand(pawn, job.targetA.Cell, true)
                || GameComponent_SandGolemTracker.Current.GolemForCaster(pawn) != null || !job.ability.CanCast);
            yield return Toils_General.Do(ChooseCastCell);
            yield return Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.OnCell);
            //基础施法任务会在完成时记录冷却，直接调用它的工作步骤以免绕过能力规则。
            foreach (Toil toil in base.MakeNewToils()) yield return toil;
        }

        //函数职责：为地面目标选择可施法的站位，避免原版面向实体的射击寻位忽略地面坐标。
        private void ChooseCastCell()
        {
            if (job.verbToUse.CanHitTarget(job.targetA))
            {
                job.SetTarget(TargetIndex.C, pawn.Position);
                return;
            }
            foreach (IntVec3 offset in GenAdj.AdjacentCells)
            {
                IntVec3 cell = job.targetA.Cell + offset;
                if (!cell.InBounds(Map) || !cell.Standable(Map) || cell.IsForbidden(pawn)
                    || !job.verbToUse.CanHitTargetFrom(cell, job.targetA) || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some)) continue;
                job.SetTarget(TargetIndex.C, cell);
                return;
            }
            EndJobWith(JobCondition.Incompletable);
        }
    }
}
