using System;
using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;

namespace NingshaRaceLib.UI.Windows
{
    //类职责：以警示铭文呈现不可逆操作，确保确认动作最多派发一次并保留确认与取消快捷键。
    public sealed class Dialog_NingshaConfirmation : NingshaWindow
    {
        private readonly string title;
        private readonly string message;
        private readonly string confirmLabel;
        private readonly Action confirmed;
        private bool dispatched;
        private Vector2 scroll;

        //构造职责：记录警示文本和操作回调，暂停游戏并吸收窗口外输入。
        public Dialog_NingshaConfirmation(string title, string message, Action confirmed, string confirmLabel = "确认继续")
        {
            this.title = title;
            this.message = message;
            this.confirmed = confirmed;
            this.confirmLabel = confirmLabel;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            closeOnCancel = false;
            forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
        }

        //属性职责：为完整警告和双操作按钮提供受屏幕约束的阅读空间。
        public override Vector2 InitialSize => new Vector2(Mathf.Min(600f, Verse.UI.screenWidth), Mathf.Min(390f, Verse.UI.screenHeight));

        //函数职责：组合警示标题、可滚动说明和确认取消双按钮。
        public override void DoWindowContents(Rect inRect)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Rect area = DrawShell(inRect, title, "请阅读以下说明，确认后再继续。", canClose: false);
                Rect body = NingshaLayout.BodyWithFooter(area, NingshaLayout.RowHeight(padding: 16f), out Rect footer);
                float width = body.width - 18f;
                Rect view = new Rect(0f, 0f, width, Mathf.Max(body.height, NingshaLayout.TextHeight(message, width)));
                Widgets.BeginScrollView(body, ref scroll, view);
                try { NingshaText.Paragraph(view, message, NingshaPalette.Ink); }
                finally { Widgets.EndScrollView(); }
                if (NingshaButton.Draw(NingshaLayout.Column(footer, 0, 2), "返回", "confirmation:cancel")) Close();
                if (NingshaButton.Draw(NingshaLayout.Column(footer, 1, 2), confirmLabel, "confirmation:confirm", destructive: true)) Confirm();
            }
        }

        //函数职责：防止同一确认窗口重复调用业务动作，异常仍交由游戏日志处理。
        private void Confirm()
        {
            if (dispatched) return;
            dispatched = true;
            confirmed();
            Close();
        }

        //函数职责：确认键沿用原版确认语义，并消费本次键盘事件。
        public override void OnAcceptKeyPressed()
        {
            Confirm();
            Event.current.Use();
        }

        //函数职责：取消键关闭窗口而不派发业务动作。
        public override void OnCancelKeyPressed()
        {
            Close();
            Event.current.Use();
        }
    }
}
