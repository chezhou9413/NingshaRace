using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.AltarMissions.World;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Windows;

namespace NingshaRaceLib.AltarMissions.UI
{
    //类职责：在原版任务接受行中为尚未接受的智慧之蛇指引加入专用拒绝按钮。
    [HarmonyPatch(typeof(MainTabWindow_Quests), "DoAcceptButton")]
    public static class Patch_MainTabWindow_Quests_RejectAltarMission
    {
        private const float ButtonWidth = 180f;
        private const float ButtonHeight = 40f;
        private const float ButtonGap = 10f;
        private const float RowAdvance = 44f;
        private const float OriginalTopGap = 17f;

        //函数职责：记录原版接受按钮绘制前的纵向位置，供后置按钮使用同一行。
        public static void Prefix(ref float curY, out float __state)
        {
            __state = curY;
        }

        //函数职责：按原版按钮占位和可用宽度绘制拒绝按钮，并在确认后结束祭坛任务。
        public static void Postfix(Rect innerRect, ref float curY, Quest ___selected, float __state)
        {
            AltarMissionWorldComponent component = AltarMissionWorldComponent.Current;
            if (___selected == null || ___selected.State != QuestState.NotYetAccepted
                || component == null || !component.IsRegisteredMission(___selected))
            {
                return;
            }

            int occupiedSlots = OccupiedOriginalButtonSlots(___selected);
            float buttonX = innerRect.x + occupiedSlots * (ButtonWidth + ButtonGap);
            float buttonY = __state + OriginalTopGap;
            if (occupiedSlots == 0)
            {
                curY = Mathf.Max(curY, buttonY + RowAdvance);
            }
            else if (buttonX + ButtonWidth > innerRect.xMax)
            {
                buttonX = innerRect.x;
                buttonY = curY;
                curY += RowAdvance;
            }

            using (new NingshaGuiScope(GameFont.Small))
            {
                if (NingshaButton.Draw(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "拒绝指引",
                    "altar:reject:" + ___selected.id, tip: "拒绝本次指引，已消耗的供奉不会返还。", destructive: true))
                {
                    OpenRejectConfirmation(___selected);
                }
            }
        }

        //函数职责：根据选择任务与开发者模式计算原版接受行已经使用的按钮槽位。
        private static int OccupiedOriginalButtonSlots(Quest quest)
        {
            bool hasChoicePart = false;
            List<QuestPart> parts = quest.PartsListForReading;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] is QuestPart_Choice)
                {
                    hasChoicePart = true;
                    break;
                }
            }

            if (hasChoicePart)
            {
                return Prefs.DevMode ? 1 : 0;
            }
            return Prefs.DevMode ? 2 : 1;
        }

        //函数职责：打开明确提示供奉不退款的确认框，并在成功拒绝后给予玩家反馈。
        private static void OpenRejectConfirmation(Quest quest)
        {
            string text = "确定拒绝智慧之蛇的本次指引吗？\n\n本次供奉已经消耗的100点营养不会返还。";
            Find.WindowStack.Add(new Dialog_NingshaConfirmation("拒绝智慧之蛇指引", text, delegate
            {
                if (AltarMissionWorldComponent.Current?.TryRejectOffer(quest) == true)
                {
                    Messages.Message("已拒绝本次指引。再次供奉后，可以向智慧之蛇寻求新的指引。", MessageTypeDefOf.NeutralEvent, false);
                }
            }, "确认拒绝指引"));
        }
    }
}
