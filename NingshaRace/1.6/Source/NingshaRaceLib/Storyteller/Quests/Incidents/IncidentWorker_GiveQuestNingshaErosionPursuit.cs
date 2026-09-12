using System;
using RimWorld;
using RimWorld.QuestGen;
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

            QuestScriptDef questDef = def.questScriptDef;
            if (!questDef.CanRun(parms.points, parms.target)) return false;
            bool generated = false;
            Slate slate = new Slate();
            slate.Set("points", parms.points);
            //原版 QuestNode 会记录并吞掉异常，必须等根节点完成全部部件后才发布任务。
            slate.Set("ningshaPursuitGenerated", (Action)(() => generated = true));
            Quest quest = QuestGen.Generate(questDef, slate);
            if (!generated)
            {
                quest?.CleanupQuestParts();
                Log.Error("凝砂侵蚀追杀任务生成未完成，未发布任务，也未消耗一次性任务机会。请检查此前的生成异常。");
                return false;
            }
            Find.QuestManager.Add(quest);
            state.MarkOfferConsumed();
            if (!quest.hidden && questDef.sendAvailableLetter) QuestUtility.SendLetterQuestAvailable(quest);
            return true;
        }
    }
}
