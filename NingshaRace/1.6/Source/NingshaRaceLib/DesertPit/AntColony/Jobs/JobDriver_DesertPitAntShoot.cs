using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Components;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //吐酸蚁沿用原版射击结算，并在出手前检查撤退防御范围。
    public sealed class JobDriver_DesertPitAntShoot : JobDriver_AttackStatic
    {
        //常驻遇敌检查保持同一目标的预热，目标改变时允许切换攻击。
        public override bool IsContinuation(Job next)
        {
            return job.targetA == next.targetA && job.verbToUse == next.verbToUse;
        }

        //固定帧与攻击间隔帧均验证目标，防止预热期间目标已越界。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Map.GetComponent<MapComponent_DesertPitAntColonies>()
                .CanContinueColonyAttack(pawn, TargetA.Thing));
            foreach (Toil toil in base.MakeNewToils())
            {
                if (toil.tickIntervalAction != null) toil.AddPreTickIntervalAction(CheckBeforeShot);
                yield return toil;
            }
        }

        //撤退目标离开半径或藏到墙后时结束本次射击。
        private void CheckBeforeShot(int delta)
        {
            if (!Map.GetComponent<MapComponent_DesertPitAntColonies>().CanContinueColonyAttack(pawn, TargetA.Thing))
                EndJobWith(JobCondition.Incompletable);
        }
    }
}
