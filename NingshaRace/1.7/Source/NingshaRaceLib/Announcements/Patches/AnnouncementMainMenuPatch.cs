using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using NingshaRaceLib.Announcements.Settings;
using NingshaRaceLib.Announcements.UI;

namespace NingshaRaceLib.Announcements.Patches
{
    //类职责：在主菜单首次绘制时检查本地未读公告，每次启动最多自动打开一次。
    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI))]
    internal static class AnnouncementMainMenuPatch
    {
        private static bool checkedThisSession;

        //函数职责：等待进入主菜单后显示未读公告，窗口创建和资源绘制均留在主线程。
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (checkedThisSession || Current.ProgramState != ProgramState.Entry
                || Event.current.type != EventType.Repaint) return;
            NingshaAnnouncementsMod mod = NingshaAnnouncementsMod.Instance;
            if (mod == null) return;
            checkedThisSession = true;
            if (!mod.AnnouncementSettings.autoPopup) return;
            var unread = NingshaAnnouncementsMod.GetAnnouncements()
                .FirstOrDefault(item => !mod.AnnouncementSettings.readKeys.Contains(item.ReadKey));
            if (unread != null && !Find.WindowStack.IsOpen<Dialog_NingshaAnnouncements>())
                Find.WindowStack.Add(new Dialog_NingshaAnnouncements(unread));
        }
    }
}
