using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Components;
using RimWorld;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //工蚁在脚下短暂挖掘，再结算半天一次的资源产出。
    public sealed class JobDriver_DesertPitAntDigResource : JobDriver
    {
        //只预留脚下地格，避免挖掘期间与其他工作争用。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        //挖掘期间播放原版掘地特效，受到中断时不结算产物或重置计时。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.Downed || pawn.Dead || pawn.Position != TargetA.Cell);
            Toil dig = Toils_General.Wait(180).WithProgressBarToilDelay(TargetIndex.A);
            dig.WithEffect(EffecterDefOf.Mine, TargetIndex.A);
            yield return dig;
            yield return Toils_General.Do(() => Map.GetComponent<MapComponent_DesertPitAntColonies>().CompleteResourceDig(pawn));
        }
    }
}
