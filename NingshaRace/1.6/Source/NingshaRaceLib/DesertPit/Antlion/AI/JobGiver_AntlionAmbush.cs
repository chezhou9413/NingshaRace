using NingshaRaceLib.DesertPit.Antlion.Components;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Antlion.AI
{
    //类职责：把伏击组件的当前阶段转换为单一任务，不调用普通动物的觅食或自动狩猎。
    public sealed class JobGiver_AntlionAmbush : ThinkNode_JobGiver
    {
        //函数职责：在原版应急行为未接管时获取蚁狮追击、回沙地或等待任务。
        protected override Job TryGiveJob(Pawn pawn)
        {
            return pawn.GetComp<CompAntlionAmbush>().CreateJob();
        }
    }
}
