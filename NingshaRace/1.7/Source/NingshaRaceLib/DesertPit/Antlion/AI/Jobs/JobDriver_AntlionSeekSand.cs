using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Antlion.Components;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Antlion.AI.Jobs
{
    //类职责：让蚁狮实际步行到合法沙地，到达后通知组件进入下潜动画。
    public sealed class JobDriver_AntlionSeekSand : JobDriver
    {
        //函数职责：不抢占资源，目的地占用变化由每次任务检查处理。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        //函数职责：沿原版路径行走并验证目的地，下潜动作只在真实到达后触发。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompAntlionAmbush comp = pawn.GetComp<CompAntlionAmbush>();
            AddEndCondition(() => comp.CanContinueReturn(job.targetA.Cell)
                ? JobCondition.Ongoing : JobCondition.InterruptForced);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            yield return Toils_General.DoAtomic(comp.NotifyReachedSand);
        }

        //函数职责：延后沙地重试并终止当前路径，避免对受阻格持续重发任务。
        public override void Notify_PatherFailed()
        {
            pawn.GetComp<CompAntlionAmbush>().NotifySandPathFailed();
            base.Notify_PatherFailed();
        }
    }
}
