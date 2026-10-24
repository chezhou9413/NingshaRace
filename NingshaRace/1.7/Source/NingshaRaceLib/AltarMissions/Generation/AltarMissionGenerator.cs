using System;
using RimWorld;
using Verse;

using NingshaRaceLib.AltarMissions.Core;
using NingshaRaceLib.AltarMissions.World;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.AltarMissions.Generation
{
    //类职责：等概率选择三类祭坛任务并通过原版任务系统发布和登记。
    public static class AltarMissionGenerator
    {
        //函数职责：确认全局任务槽空闲后生成任务、发送任务信并登记其编号。
        public static bool TryGenerateRandomMission(Pawn consulter)
        {
            return TryGenerateMission((AltarMissionType)Rand.Range(0, 3), consulter);
        }

        //函数职责：按指定类型生成祭坛任务，并复用全局任务槽登记与原版任务信发布流程。
        public static bool TryGenerateMission(AltarMissionType missionType, Pawn consulter)
        {
            AltarMissionWorldComponent component = AltarMissionWorldComponent.Current;
            if (component == null || component.HasActiveMission)
            {
                return false;
            }
            QuestScriptDef root = QuestDefFor(missionType);
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(root, StorytellerUtility.DefaultSiteThreatPointsNow());
            if (quest == null)
            {
                throw new InvalidOperationException("智慧之蛇祭坛未能生成有效任务。");
            }
            if (!component.TryRegister(quest))
            {
                Find.QuestManager.Remove(quest);
                throw new InvalidOperationException("智慧之蛇祭坛任务生成后无法登记全局任务槽。");
            }
            QuestUtility.SendLetterQuestAvailable(quest, consulter?.LabelShortCap + "完成了祭坛祈求");
            return true;
        }

        //函数职责：把祭坛任务类型映射为负责生成对应世界地点的任务定义。
        private static QuestScriptDef QuestDefFor(AltarMissionType missionType)
        {
            switch (missionType)
            {
                case AltarMissionType.SmallRuins: return DefOfRefs.NingshaRace_Quest_AltarSmallRuins;
                case AltarMissionType.AntNest: return DefOfRefs.NingshaRace_Quest_AltarAntNest;
                case AltarMissionType.RescueKinsfolk: return DefOfRefs.NingshaRace_Quest_AltarRescueKinsfolk;
                default: throw new ArgumentOutOfRangeException(nameof(missionType), missionType, "未知的智慧之蛇祭坛任务类型。");
            }
        }
    }
}
