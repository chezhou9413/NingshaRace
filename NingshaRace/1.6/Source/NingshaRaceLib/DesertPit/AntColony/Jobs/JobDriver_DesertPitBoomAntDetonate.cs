using System.Collections.Generic;
using Verse;
using Verse.AI;

using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.Pawns;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //类职责：驱动爆浆蚁追踪外来 Pawn 或敌方巢穴，并在进入配置距离后触发自爆死亡。
    public class JobDriver_DesertPitBoomAntDetonate : JobDriver
    {
        private const TargetIndex IntruderIndex = TargetIndex.A;

        //函数职责：爆浆蚁允许共同追击同一目标，因此不独占预留目标。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        //函数职责：追到目标接触范围，验证实际距离并触发爆浆蚁的单次爆炸流程。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(IntruderIndex);
            yield return Toils_Goto.GotoThing(IntruderIndex, PathEndMode.Touch);
            yield return Toils_General.Do(delegate
            {
                Thing target = job.GetTarget(IntruderIndex).Thing;
                MapComponent_DesertPitAntColonies manager = Map.GetComponent<MapComponent_DesertPitAntColonies>();
                if (!manager.IsBoomInTriggerRange(pawn, target))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                ((Pawn_DesertPitBoomAnt)pawn).Detonate();
            });
        }
    }
}
