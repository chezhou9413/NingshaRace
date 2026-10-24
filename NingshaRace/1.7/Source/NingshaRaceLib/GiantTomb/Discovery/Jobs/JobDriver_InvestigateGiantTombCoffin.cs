using System.Collections.Generic;
using Verse;
using Verse.AI;

using NingshaRaceLib.GiantTomb.Discovery.Buildings;

namespace NingshaRaceLib.GiantTomb.Discovery.Jobs
{
    //类职责：让玩家指定的殖民者预约、接近并调查破损石棺，完成后揭示巨型墓葬入口。
    public sealed class JobDriver_InvestigateGiantTombCoffin : JobDriver
    {
        //字段职责：规定调查破损石棺需要持续的 Tick 数量。
        private const int InvestigateTicks = 300;

        //属性职责：取得当前工作目标对应的未调查破损石棺。
        private Building_GiantTombBrokenCoffin Coffin => job.targetA.Thing as Building_GiantTombBrokenCoffin;

        //函数职责：在工作开始前独占预约破损石棺，阻止多名殖民者重复调查。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        //函数职责：依次执行接近、带进度条调查和入口揭示三个阶段。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);

            Toil investigate = Toils_General.WaitWith(TargetIndex.A, InvestigateTicks, true, true, false, TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return investigate;

            yield return Toils_General.Do(delegate
            {
                Coffin?.RevealEntrance(pawn);
            });
        }
    }
}
