using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Announcements.Settings
{
    //类职责：跨存档保存本机公告已读记录与自动弹出偏好。
    public sealed class NingshaAnnouncementSettings : ModSettings
    {
        public bool autoPopup = true;
        public List<string> readKeys = new List<string>();

        //函数职责：读写模组设置中的公告偏好和已读集合。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref autoPopup, "announcementAutoPopup", true);
            Scribe_Collections.Look(ref readKeys, "announcementReadKeys", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && readKeys == null) readKeys = new List<string>();
        }
    }
}
