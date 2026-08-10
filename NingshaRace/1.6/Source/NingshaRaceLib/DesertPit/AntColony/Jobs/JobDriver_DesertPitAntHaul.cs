using System.Collections.Generic;
using Verse;
using Verse.AI;

using NingshaRaceLib.DesertPit.AntColony.Components;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //类职责：让工蚁把已分配的实体物资搬到巢群专属储藏格。
    public class JobDriver_DesertPitAntHaul : JobDriver
    {
        private const TargetIndex HaulableIndex = TargetIndex.A;
        private const TargetIndex StorageCellIndex = TargetIndex.B;

        //函数职责：预留搬运物和目标储藏格，避免多只工蚁互相争抢。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(HaulableIndex), job, 1, job.count, null, errorOnFailed) &&
                   pawn.Reserve(job.GetTarget(StorageCellIndex), job, 1, -1, null, errorOnFailed);
        }

        //函数职责：依次前往物资、拿起指定数量、搬到巢穴储藏格并释放运行时分配。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(HaulableIndex);
            this.FailOn(delegate
            {
                Thing thing = job.GetTarget(HaulableIndex).Thing ?? pawn.carryTracker.CarriedThing;
                return thing == null || !Map.GetComponent<MapComponent_DesertPitAntColonies>().IsStorageCellAvailableFor(pawn, job.GetTarget(StorageCellIndex).Cell, thing);
            });

            yield return Toils_Goto.GotoThing(HaulableIndex, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(HaulableIndex, false, true, false, true);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StorageCellIndex, PathEndMode.OnCell);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(StorageCellIndex, carryToCell, false);
            yield return Toils_General.Do(delegate
            {
                Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyForageCompleted(pawn);
            });
        }
    }
}
