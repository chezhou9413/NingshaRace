using System.Collections.Generic;
using Verse;
using Verse.AI;

using NingshaRaceLib.Altar.Components;

namespace NingshaRaceLib.Altar.Jobs
{
    //类职责：搬运指定数量生肉到祭坛并将其不可逆地转化为供奉营养。
    public sealed class JobDriver_FillWisdomSerpentAltar : JobDriver
    {
        private const TargetIndex MeatIndex = TargetIndex.A;
        private const TargetIndex AltarIndex = TargetIndex.B;

        //属性职责：取得工作指定的智慧之蛇祭坛供奉组件。
        private CompAltarOffering AltarComp => job.GetTarget(AltarIndex).Thing?.TryGetComp<CompAltarOffering>();

        //函数职责：在工作开始前预留生肉与祭坛。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(MeatIndex).Thing, job, 1, job.count, null, errorOnFailed)
                && pawn.Reserve(job.GetTarget(AltarIndex).Thing, job, 1, 1, null, errorOnFailed);
        }

        //函数职责：依次接近生肉、携带供品、到达祭坛并完成营养转化。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(MeatIndex);
            this.FailOnDestroyedOrNull(AltarIndex);
            this.FailOnForbidden(MeatIndex);
            this.FailOnForbidden(AltarIndex);
            this.FailOn(delegate { return AltarComp == null || !AltarComp.CanAcceptOffering; });
            yield return Toils_Goto.GotoThing(MeatIndex, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(MeatIndex);
            yield return Toils_Haul.StartCarryThing(MeatIndex, false, true);
            yield return Toils_Goto.GotoThing(AltarIndex, PathEndMode.Touch);
            Toil offer = ToilMaker.MakeToil("OfferRawMeatToWisdomSerpentAltar");
            offer.initAction = delegate
            {
                Thing meat = pawn.carryTracker.CarriedThing;
                if (meat == null || AltarComp.ConsumeRawMeat(meat) <= 0)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };
            offer.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return offer;
        }
    }
}
