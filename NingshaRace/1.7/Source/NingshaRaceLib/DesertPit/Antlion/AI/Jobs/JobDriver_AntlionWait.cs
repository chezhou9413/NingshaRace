using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Antlion.AI.Jobs
{
    //类职责：提供不自动反击的静止任务，避免出入沙地时原版等待任务攻击机械体。
    public sealed class JobDriver_AntlionWait : JobDriver
    {
        //函数职责：等待不预定地面资源，由伏击组件决定何时中断。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        //函数职责：保持原地且不调用自动目标搜索，依靠任务期限重新评估状态。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil wait = ToilMaker.MakeToil("蚁狮等待");
            wait.initAction = () => pawn.pather.StopDead();
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            yield return wait;
        }
    }
}
