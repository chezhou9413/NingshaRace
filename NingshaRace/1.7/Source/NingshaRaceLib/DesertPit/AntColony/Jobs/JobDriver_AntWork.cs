using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.State;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //工蚁和蚁后只执行巢群已分配的任务，进度存放在巢群任务记录。
    public sealed class JobDriver_AntWork : JobDriver
    {
        private MapComponent_DesertPitAntColonies Manager => Map.GetComponent<MapComponent_DesertPitAntColonies>();

        //巢群目标由任务独占，原版预留限制为执行者占用的交互格。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Manager.TaskFor(pawn) != null;

        //中断先释放预留；完成时由管理器进行完整实物结算。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            MapComponent_DesertPitAntColonies manager = Manager;
            AntColonyTask task = manager.TaskFor(pawn);
            AddFinishAction(condition => manager.ReleaseTask(pawn, task));
            this.FailOn(() => !manager.TaskIsValid(pawn) || pawn.Downed || pawn.Dead);
            if (task.Kind != AntTaskKind.Dig) yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil work = ToilMaker.MakeToil("蚁群施工");
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.tickAction = delegate
            {
                AntColonyTask current = manager.TaskFor(pawn);
                if (current == null) { EndJobWith(JobCondition.Incompletable); return; }
                //原版六级建造速度为 0.3 + 6 × 0.0875；孵化和定时发掘按实际 tick。
                current.Progress += current.Kind == AntTaskKind.Hatch || current.Kind == AntTaskKind.Dig ? 1f : 0.825f;
                pawn.rotationTracker.FaceCell(current.Cell);
                if (current.Progress >= current.Work)
                    EndJobWith(manager.CompleteTask(pawn) ? JobCondition.Succeeded : JobCondition.Incompletable);
            };
            work.WithProgressBar(TargetIndex.A, () => task.Progress / task.Work);
            if (task.Kind == AntTaskKind.Dig) work.WithEffect(EffecterDefOf.Mine, TargetIndex.A);
            yield return work;
        }
    }
}
