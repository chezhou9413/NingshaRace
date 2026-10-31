using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.State;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //地面与容器之间共用实物搬运流程，任务中断不吞掉携带物。
    public sealed class JobDriver_DesertPitAntHaul : JobDriver
    {
        private MapComponent_DesertPitAntColonies Manager => Map.GetComponent<MapComponent_DesertPitAntColonies>();

        //地面物品使用原版预留；容器内部物品由巢群任务独占。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Manager.TaskFor(pawn) != null
            && pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);

        //取货前检查危险与驱蚁区，取货后优先把实际携带物送回。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            MapComponent_DesertPitAntColonies manager = Manager;
            AntColonyTask assigned = manager.TaskFor(pawn);
            AddFinishAction(condition => manager.ReleaseTask(pawn, assigned));
            this.FailOn(() => !manager.TaskIsValid(pawn));
            this.FailOn(() => pawn.carryTracker.CarriedThing == null &&
                (manager.IsWorkerForageDangerous(pawn, TargetA.Cell) || Map.GetComponent<MapComponent_AntHabitats>().IsRepelled(TargetA.Cell)));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Do(delegate
            {
                AntColonyTask task = manager.TaskFor(pawn);
                Thing source = task.Target;
                int count = System.Math.Min(job.count, source.stackCount);
                if (source.holdingOwner != null)
                    source.holdingOwner.TryTransferToContainer(source, pawn.carryTracker.innerContainer, count);
                else pawn.carryTracker.TryStartCarry(source, count);
                if (pawn.carryTracker.CarriedThing == null) EndJobWith(JobCondition.Incompletable);
            });
            yield return Toils_Goto.GotoThing(TargetIndex.B, TargetB.HasThing ? PathEndMode.Touch : PathEndMode.OnCell);
            yield return Toils_General.Do(delegate
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null) { EndJobWith(JobCondition.Incompletable); return; }
                bool delivered;
                if (TargetB.Thing is Building_AntStoragePit pit)
                    delivered = pit.Receive(carried, carried.stackCount) > 0;
                else delivered = manager.IsStorageCellAvailableFor(pawn, TargetB.Cell, carried)
                    && pawn.carryTracker.TryDropCarriedThing(TargetB.Cell, ThingPlaceMode.Direct, out Thing dropped);
                if (delivered) manager.NotifyForageCompleted(pawn);
                else EndJobWith(JobCondition.Incompletable);
            });
        }
    }
}
