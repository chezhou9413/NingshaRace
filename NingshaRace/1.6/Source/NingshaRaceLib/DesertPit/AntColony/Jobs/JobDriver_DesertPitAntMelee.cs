using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Combat;
using NingshaRaceLib.DesertPit.AntColony.Components;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //类职责：复用原版近战追击与结算，在攻击能力消失时结束蚁群任务，避免空招式循环攻击。
    public sealed class JobDriver_DesertPitAntMelee : JobDriver_AttackMelee
    {
        //函数职责：保留原版所有动作，分别在固定帧与间隔攻击帧之前检查目标和近战能力。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !CanContinueAttack());
            foreach (Toil toil in base.MakeNewToils())
            {
                //原版间隔帧不调用全局失败条件，实际出手动作必须另外在前置回调中拦截。
                if (toil.tickIntervalAction != null) toil.AddPreTickIntervalAction(CheckBeforeAttack);
                yield return toil;
            }
        }

        //函数职责：在原版追击攻击回调之前取消已无法出手的任务，不吞异常、不修改全局近战选择器。
        private void CheckBeforeAttack(int delta)
        {
            if (!CanContinueAttack()) EndJobWith(JobCondition.Incompletable);
        }

        //巢群战斗检查撤退边界，单独派发的开路工作不受防御半径限制。
        private bool CanContinueAttack()
        {
            return AntMeleeCapability.CanAttack(pawn, job.targetA.Thing)
                && (!job.targetB.IsValid || Map.GetComponent<MapComponent_DesertPitAntColonies>()
                    .CanContinueColonyAttack(pawn, job.targetA.Thing));
        }
    }
}
