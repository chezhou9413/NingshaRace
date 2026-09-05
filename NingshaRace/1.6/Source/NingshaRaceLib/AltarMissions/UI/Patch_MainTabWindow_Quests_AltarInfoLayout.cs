using HarmonyLib;
using RimWorld;

using NingshaRaceLib.AltarMissions.World;

namespace NingshaRaceLib.AltarMissions.UI
{
    //类职责：把祭坛任务的右对齐时间信息放到按钮之后，避免与接受和拒绝按钮重叠。
    [HarmonyPatch(typeof(MainTabWindow_Quests), "DoRightAlignedInfo")]
    public static class Patch_MainTabWindow_Quests_AltarInfoLayout
    {
        //函数职责：仅对当前尚未接受的祭坛任务使用按钮绘制后的纵坐标作为时间信息起点。
        public static void Prefix(float curY, ref float curYBeforeAcceptButton, Quest ___selected)
        {
            if (___selected != null && ___selected.State == QuestState.NotYetAccepted
                && AltarMissionWorldComponent.Current?.IsRegisteredMission(___selected) == true)
            {
                curYBeforeAcceptButton = curY;
            }
        }
    }
}
