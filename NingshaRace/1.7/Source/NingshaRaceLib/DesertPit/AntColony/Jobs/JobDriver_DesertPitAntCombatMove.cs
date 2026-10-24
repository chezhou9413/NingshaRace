using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Components;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //吐酸蚁移动到射击点时持续验证追击目标。
    public sealed class JobDriver_DesertPitAntCombatMove : JobDriver_Goto
    {
        //同一敌人继续前往既定射击点，目标变化时重新选择位置。
        public override bool IsContinuation(Job next)
        {
            return job.targetA == next.targetA && job.targetB == next.targetB;
        }

        //敌人离开撤退防御范围或被墙遮挡后停止寻找射击位置。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Map.GetComponent<MapComponent_DesertPitAntColonies>()
                .CanContinueColonyAttack(pawn, TargetB.Thing));
            foreach (Toil toil in base.MakeNewToils()) yield return toil;
        }
    }
}
