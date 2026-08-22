using RimWorld;
using Verse;

using NingshaRaceLib.Storyteller.Quests.Scheduling;

namespace NingshaRaceLib.Storyteller.Quests.Incidents
{
    //类职责：生成固定侵蚀追杀任务，并确保整个存档只能成功提供一次任务信。
    public sealed class IncidentWorker_GiveQuestNingshaErosionPursuit : IncidentWorker_GiveQuest
    {
        //函数职责：阻止已经消耗、缺少玩家主地图或缺少实体阵营的任务事件进入候选池。
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            WorldComponent_NingshaErosionPursuit state =
                Find.World?.GetComponent<WorldComponent_NingshaErosionPursuit>();
            return state != null
                && !state.OfferConsumed
                && Find.AnyPlayerHomeMap != null
                && Faction.OfEntities != null
                && base.CanFireNowSub(parms);
        }

        //函数职责：在任务实际创建后立即写入一次性标记，并在同一 tick 的重复请求间再次校验状态。
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            WorldComponent_NingshaErosionPursuit state =
                Find.World?.GetComponent<WorldComponent_NingshaErosionPursuit>();
            if (state == null || state.OfferConsumed)
            {
                return false;
            }

            bool executed = base.TryExecuteWorker(parms);
            if (executed)
            {
                state.MarkOfferConsumed();
            }
            return executed;
        }
    }
}
