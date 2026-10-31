using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //蚁后避难和爆浆蚁休眠使用相同入巢任务。
    public sealed class JobDriver_AntEnterNest : JobDriver
    {
        //多只成员可以同时进入，不独占整座巢穴。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        //必须走到巢穴入口才能隐藏。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Do(() => ((Building_DesertPitAntNest)TargetA.Thing).Enter(pawn));
        }
    }
}
