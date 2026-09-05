using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;

namespace NingshaRaceLib.UI.Windows
{
    //类职责：展开状态石板的完整数值和规则说明，避免紧凑 Gizmo 被迫塞入过多文字。
    public sealed class Dialog_NingshaReadout : NingshaWindow
    {
        private readonly string title;
        private readonly string body;
        private Vector2 scroll;

        //构造职责：记录本次点开时的状态快照并允许关闭及拖动。
        public Dialog_NingshaReadout(string title, string body)
        {
            this.title = title;
            this.body = body;
            draggable = true;
            closeOnAccept = true;
            closeOnCancel = true;
        }

        //属性职责：给规则说明提供可阅读且不超出屏幕的窗口尺寸。
        public override Vector2 InitialSize => new Vector2(Mathf.Min(560f, Verse.UI.screenWidth), Mathf.Min(420f, Verse.UI.screenHeight));

        //函数职责：组合标题、可滚动铭文和关闭按钮，并保证输入与绘制状态归还调用者。
        public override void DoWindowContents(Rect inRect)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Rect area = DrawShell(inRect, title, "这里显示打开窗口时的数值。");
                Rect bodyRect = NingshaLayout.BodyWithFooter(area, NingshaLayout.RowHeight(padding: 16f), out Rect footer);
                float width = bodyRect.width - 18f;
                Rect view = new Rect(0f, 0f, width, Mathf.Max(bodyRect.height, NingshaLayout.TextHeight(body, width)));
                Widgets.BeginScrollView(bodyRect, ref scroll, view);
                try { NingshaText.Paragraph(view, body, NingshaPalette.Ink); }
                finally { Widgets.EndScrollView(); }
                if (NingshaButton.Draw(footer, "关闭", "readout:" + ID)) Close();
            }
        }
    }
}
