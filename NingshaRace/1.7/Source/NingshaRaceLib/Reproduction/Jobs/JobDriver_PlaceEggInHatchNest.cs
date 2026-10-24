using System.Collections.Generic;
using Verse;
using Verse.AI;

using NingshaRaceLib.Reproduction.Buildings;

namespace NingshaRaceLib.Reproduction.Jobs
{
    //类职责：让搬运者拿起一枚凝砂卵并将其转移到指定孵化巢容器中。
    public class JobDriver_PlaceEggInHatchNest : JobDriver
    {
        //字段职责：标识工作目标中的凝砂卵。
        private const TargetIndex EggIndex = TargetIndex.A;

        //字段职责：标识工作目标中的凝砂孵化巢。
        private const TargetIndex NestIndex = TargetIndex.B;

        //属性职责：取得工作当前指定的凝砂卵。
        private Thing Egg => job.GetTarget(EggIndex).Thing;

        //属性职责：取得工作当前指定的凝砂孵化巢。
        private Building_NingshaHatchNest Nest => job.GetTarget(NestIndex).Thing as Building_NingshaHatchNest;

        //函数职责：在工作开始前分别预留凝砂卵与孵化巢。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Egg, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(Nest, job, 1, 1, null, errorOnFailed);
        }

        //函数职责：依次执行接近卵、拿起一枚、移动到巢旁并转移到巢内的工作步骤。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(EggIndex);
            this.FailOnDestroyedOrNull(NestIndex);
            this.FailOnForbidden(EggIndex);
            this.FailOnForbidden(NestIndex);
            this.FailOn(delegate { return Nest == null || !Nest.Empty; });

            yield return Toils_Goto.GotoThing(EggIndex, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(EggIndex);
            yield return Toils_Haul.StartCarryThing(EggIndex, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
            yield return Toils_Goto.GotoThing(NestIndex, PathEndMode.Touch);

            Toil placeEgg = ToilMaker.MakeToil("PlaceNingshaEggInHatchNest");
            placeEgg.initAction = delegate
            {
                Thing carriedEgg = pawn.carryTracker.CarriedThing;
                if (carriedEgg == null || !Nest.TryAcceptEgg(carriedEgg))
                {
                    Log.Error("[NingshaRace] 搬运者无法把凝砂卵放入指定孵化巢。");
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };
            placeEgg.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return placeEgg;
        }
    }
}
