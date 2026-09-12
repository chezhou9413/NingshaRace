using System;
using UnityEngine;
using Verse;
using NingshaRaceLib.Announcements.Data;
using NingshaRaceLib.Announcements.Settings;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;
using NingshaRaceLib.UI.Windows;

namespace NingshaRaceLib.Announcements.UI
{
    //类职责：以凝砂窗口样式展示本地公告，并提供历史翻阅与正文滚动。
    public sealed class Dialog_NingshaAnnouncements : NingshaWindow
    {
        private readonly NingshaAnnouncementDef[] announcements;
        private int selected;
        private Vector2 scroll;

        //构造职责：读取公告列表，优先定位指定未读公告并配置可暂停关闭的窗口。
        public Dialog_NingshaAnnouncements(NingshaAnnouncementDef initial = null)
        {
            announcements = NingshaAnnouncementsMod.GetAnnouncements();
            selected = initial == null ? 0 : Math.Max(0, Array.IndexOf(announcements, initial));
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnCancel = true;
            closeOnAccept = true;
            draggable = true;
        }

        //属性职责：限制公告窗口大小，为中文正文和底部操作保留空间。
        public override Vector2 InitialSize => new Vector2(Mathf.Min(780f, Verse.UI.screenWidth),
            Mathf.Min(700f, Verse.UI.screenHeight));

        //函数职责：窗口真正打开后才登记当前公告已读。
        public override void PostOpen()
        {
            base.PostOpen();
            MarkCurrentRead();
        }

        //函数职责：绘制标题、按文字测量的滚动正文与固定页脚，防止长公告挤占操作区。
        public override void DoWindowContents(Rect inRect)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Rect area = DrawShell(inRect, "凝砂族 · 更新公告");
                NingshaLayout layout = new NingshaLayout(area);
                string title = announcements.Length == 0 ? "暂无公告" : announcements[selected].label;
                NingshaText.Label(layout.Take(NingshaLayout.RowHeight(padding: 8f)), title, NingshaPalette.Sand);
                string date = announcements.Length == 0 ? "" : announcements[selected].date;
                NingshaText.Label(layout.Take(NingshaLayout.RowHeight()), date, NingshaPalette.Muted);
                Rect bodyRect = NingshaLayout.BodyWithFooter(layout.Remaining,
                    NingshaLayout.RowHeight(padding: 16f), out Rect footer);
                string body = announcements.Length == 0 ? "当前版本没有附带公告。" : announcements[selected].body;
                float width = bodyRect.width - 18f;
                float height = Mathf.Max(bodyRect.height, NingshaLayout.TextHeight(body, width) + 8f);
                Widgets.BeginScrollView(bodyRect, ref scroll, new Rect(0f, 0f, width, height));
                try { NingshaText.Paragraph(new Rect(0f, 0f, width, height), body, NingshaPalette.Ink); }
                finally { Widgets.EndScrollView(); }
                if (NingshaButton.Draw(NingshaLayout.Column(footer, 0, 3), "较新公告", "news:newer", selected > 0))
                    Select(selected - 1);
                if (NingshaButton.Draw(NingshaLayout.Column(footer, 1, 3), "关闭", "news:close")) Close();
                if (NingshaButton.Draw(NingshaLayout.Column(footer, 2, 3), "较早公告", "news:older",
                    selected + 1 < announcements.Length)) Select(selected + 1);
            }
        }

        //函数职责：切换公告后重置滚动位置，并写入该篇公告的已读状态。
        private void Select(int index)
        {
            selected = index;
            scroll = Vector2.zero;
            MarkCurrentRead();
        }

        //函数职责：将当前可见正文登记为本机已读。
        private void MarkCurrentRead()
        {
            if (announcements.Length > 0) NingshaAnnouncementsMod.Instance.MarkRead(announcements[selected]);
        }
    }
}
