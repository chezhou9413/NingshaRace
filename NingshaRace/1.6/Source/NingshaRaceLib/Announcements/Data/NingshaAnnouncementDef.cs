using Verse;

namespace NingshaRaceLib.Announcements.Data
{
    //类职责：定义随模组发布的公告正文、排序与修订号。
    public sealed class NingshaAnnouncementDef : Def
    {
        public int order;
        public int revision = 1;
        public string date;
        public string body;

        //属性职责：用公告名和修订号标识一次独立的已读记录。
        public string ReadKey => defName + ":" + revision;
    }
}
