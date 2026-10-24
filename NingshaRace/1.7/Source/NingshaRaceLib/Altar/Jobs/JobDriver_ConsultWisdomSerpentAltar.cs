using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Altar.Components;

namespace NingshaRaceLib.Altar.Jobs
{
    //类职责：让殖民者在祭坛前祈求三百Tick并在结束时尝试发布任务。
    public sealed class JobDriver_ConsultWisdomSerpentAltar : JobDriver
    {
        private const TargetIndex AltarIndex = TargetIndex.A;

        //属性职责：取得当前交互目标的祭坛供奉组件。
        private CompAltarOffering AltarComp => job.GetTarget(AltarIndex).Thing?.TryGetComp<CompAltarOffering>();

        //函数职责：预留唯一的祭坛交互位置。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(AltarIndex).Thing, job, 1, 1, null, errorOnFailed);
        }

        //函数职责：接近祭坛、持续祈求三百Tick并在生成成功后消耗供奉。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(AltarIndex);
            this.FailOn(delegate { return AltarComp == null || !AltarComp.OccupiedByPlayer || !AltarComp.Full; });
            yield return Toils_Goto.GotoThing(AltarIndex, PathEndMode.Touch);
            Toil consult = Toils_General.Wait(300);
            consult.WithProgressBarToilDelay(AltarIndex);
            consult.FailOnCannotTouch(AltarIndex, PathEndMode.Touch);
            yield return consult;
            Toil issue = ToilMaker.MakeToil("IssueWisdomSerpentMission");
            issue.initAction = delegate
            {
                if (!AltarComp.TryIssueMission(pawn))
                {
                    Messages.Message("智慧之蛇没有回应；供奉未被消耗。", pawn, MessageTypeDefOf.RejectInput, false);
                }
            };
            issue.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return issue;
        }
    }
}
