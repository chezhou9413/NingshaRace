using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;

namespace NingshaRaceLib.UI.Windows
{
    //类职责：提供不接管业务生命周期的古砂岩窗口壳，统一标题、关闭区和内容边距。
    public abstract class NingshaWindow : Window
    {
        //构造职责：关闭原版背景与标题绘制，由凝砂窗口壳负责视觉和安全区。
        protected NingshaWindow()
        {
            doWindowBackground = false;
            doCloseX = false;
            doCloseButton = false;
        }

        //属性职责：由自定义窗口壳统一分配内部边距。
        protected override float Margin => 0f;

        //函数职责：绘制窗口底板和标题，返回不与标题或关闭按钮重叠的内容区。
        protected Rect DrawShell(Rect inRect, string title, string subtitle = null, bool canClose = true)
        {
            NingshaFrame.Panel(inRect);
            NingshaLayout layout = new NingshaLayout(inRect.ContractedBy(18f));
            float headerHeight = NingshaLayout.RowHeight(GameFont.Medium, 12f);
            Rect header = layout.Take(headerHeight);
            Rect titleRect = header;
            if (canClose) titleRect.xMax -= headerHeight + NingshaPalette.Gap;
            NingshaText.Label(titleRect, title, NingshaPalette.Sand, GameFont.Medium);
            if (canClose && NingshaButton.Draw(new Rect(header.xMax - headerHeight, header.y, headerHeight, headerHeight),
                "×", "close:" + ID, tip: "关闭窗口")) Close();
            if (!subtitle.NullOrEmpty())
            {
                Rect description = layout.Take(NingshaLayout.TextHeight(subtitle, header.width));
                NingshaText.Paragraph(description, subtitle);
            }
            NingshaFrame.Divider(layout.Take(5f));
            return layout.Remaining;
        }
    }
}
