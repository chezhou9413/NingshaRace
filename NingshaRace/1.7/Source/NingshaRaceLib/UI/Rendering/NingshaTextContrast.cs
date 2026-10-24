using UnityEngine;
using Verse;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：在浅沙色背景上为文字提供细窄阴影，不占用额外行高或改变原有排版。
    internal static class NingshaTextContrast
    {
        //职责：在原文字矩形内裁剪阴影与正文，各类界面事件保持一致的调用顺序并恢复颜色。
        public static void Draw(Rect rect, string text)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                Widgets.Label(rect, text);
                return;
            }
            Color foreground = GUI.color;
            Widgets.BeginGroup(rect);
            try
            {
                GUI.color = new Color(0.045f, 0.03f, 0.015f, foreground.a * 0.85f);
                Widgets.Label(new Rect(1f, 1f, rect.width, rect.height), text);
                GUI.color = foreground;
                Widgets.Label(new Rect(0f, 0f, rect.width, rect.height), text);
            }
            finally
            {
                Widgets.EndGroup();
                GUI.color = foreground;
            }
        }
    }
}
