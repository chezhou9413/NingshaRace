using System.Linq;
using UnityEngine;
using Verse;
using NingshaRaceLib.Announcements.Data;
using NingshaRaceLib.Announcements.UI;

namespace NingshaRaceLib.Announcements.Settings
{
    //类职责：提供公告设置入口，并集中管理本机已读状态。
    public sealed class NingshaAnnouncementsMod : Mod
    {
        public static NingshaAnnouncementsMod Instance;
        public readonly NingshaAnnouncementSettings AnnouncementSettings;

        //构造职责：读取本机设置，供公告窗口及主菜单提示使用。
        public NingshaAnnouncementsMod(ModContentPack content) : base(content)
        {
            Instance = this;
            AnnouncementSettings = GetSettings<NingshaAnnouncementSettings>();
        }

        //函数职责：在模组设置中登记凝砂族入口。
        public override string SettingsCategory() => "凝砂族";

        //函数职责：提供自动弹出开关及随时重读公告的入口。
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("有新公告时自动弹出", ref AnnouncementSettings.autoPopup);
            listing.Gap();
            if (listing.ButtonText("查看凝砂族更新公告"))
                Find.WindowStack.Add(new Dialog_NingshaAnnouncements());
            listing.End();
        }

        //函数职责：按发布顺序返回本地公告，供自动提示和历史翻阅共用。
        public static NingshaAnnouncementDef[] GetAnnouncements()
        {
            return DefDatabase<NingshaAnnouncementDef>.AllDefs
                .OrderByDescending(item => item.order).ThenBy(item => item.defName).ToArray();
        }

        //函数职责：正文实际打开时记录已读，避免切换存档或重启后重复提示。
        public void MarkRead(NingshaAnnouncementDef announcement)
        {
            if (AnnouncementSettings.readKeys.Contains(announcement.ReadKey)) return;
            AnnouncementSettings.readKeys.Add(announcement.ReadKey);
            WriteSettings();
        }
    }
}
