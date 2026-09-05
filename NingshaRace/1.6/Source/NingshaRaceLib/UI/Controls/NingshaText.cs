using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.UI.Controls
{
    //类职责：提供带文字测量、截断提示和颜色隔离的凝砂文本组件。
    public static class NingshaText
    {
        //函数职责：在足够行高的矩形内绘制单行文字，超宽时截断并提供完整悬停说明。
        public static void Label(Rect rect, string text, Color? color = null, GameFont font = GameFont.Small,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool tooltip = true)
        {
            using (new NingshaGuiScope(font))
            {
                text = text ?? "";
                if (rect.height < Text.LineHeightOf(font) || rect.width <= 0f)
                {
                    if (tooltip) TooltipHandler.TipRegion(rect, text);
                    return;
                }
                Text.Anchor = anchor;
                GUI.color = color ?? NingshaPalette.Ink;
                string shown = text.StripTags();
                if (Text.CalcSize(shown).x > rect.width)
                {
                    if (tooltip) TooltipHandler.TipRegion(rect, text);
                    if (Text.CalcSize("…").x > rect.width) return;
                    int low = 0;
                    int high = shown.Length;
                    while (low < high)
                    {
                        int mid = (low + high + 1) / 2;
                        if (Text.CalcSize(shown.Substring(0, mid) + "…").x <= rect.width) low = mid;
                        else high = mid - 1;
                    }
                    if (low > 0 && char.IsHighSurrogate(shown[low - 1])) low--;
                    shown = shown.Substring(0, low) + "…";
                }
                Widgets.Label(rect, shown);
            }
        }

        //函数职责：在调用方测量过的区域内绘制说明段落，保持中文换行和左上对齐。
        public static void Paragraph(Rect rect, string text, Color? color = null, GameFont font = GameFont.Small)
        {
            using (new NingshaGuiScope(font))
            {
                Text.WordWrap = true;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = color ?? NingshaPalette.Muted;
                Widgets.Label(rect, text);
            }
        }
    }
}
