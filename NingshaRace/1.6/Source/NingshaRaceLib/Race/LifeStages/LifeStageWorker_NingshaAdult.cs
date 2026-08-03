using RimWorld;
using Verse;

namespace NingshaRaceLib.Race.LifeStages
{
    //类职责：执行凝砂族进入成年阶段时的原版成长结算，并卸下仅供未成年人穿戴的服装。
    public class LifeStageWorker_NingshaAdult : LifeStageWorker_HumanlikeAdult
    {
        //函数职责：在保留原版成年阶段处理后，按发展阶段过滤器清理不允许成年人穿戴的服装。
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);

            if (Current.ProgramState != ProgramState.Playing
                || previousLifeStage == null
                || !previousLifeStage.developmentalStage.Juvenile())
            {
                return;
            }

            pawn.apparel?.DropAllOrMoveAllToInventory(
                apparel => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult));
        }
    }
}
