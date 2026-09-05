using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Rendering;

namespace NingshaRaceLib.UI.Controls
{
    //类职责：组合真实进度、细腻砂面与可读数值，不改动业务比例和完成状态。
    public static class NingshaProgress
    {
        //职责：绘制进度并按实际字体测量数值底板，空间不足时把数值保留在悬停提示中。
        public static void Draw(Rect rect, float fraction, string value, Color accent, float threshold = -1f)
        {
            using (new NingshaGuiScope(GameFont.Tiny))
            {
                NingshaProgressPainter.Draw(rect, Mathf.Clamp01(fraction), accent, threshold);
                if (!value.NullOrEmpty()) DrawValue(rect, value);
                NingshaProgressPainter.DrawThresholdEnds(rect, threshold);
            }
        }

        //职责：测量并绘制数值底板，使浅色砂面上的数字保持清晰，不挤压字体行高。
        private static void DrawValue(Rect rect, string value)
        {
            float line = Text.LineHeightOf(GameFont.Tiny);
            if (rect.height < line + 4f || rect.width < 12f)
            {
                TooltipHandler.TipRegion(rect, value);
                return;
            }
            float width = Mathf.Min(rect.width - 6f, Text.CalcSize(value).x + 10f);
            Rect label = new Rect(rect.center.x - width * 0.5f, rect.center.y - (line + 2f) * 0.5f, width, line + 2f);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.color = new Color(0.04f, 0.035f, 0.025f, 0.7f);
                Widgets.DrawAtlas(label, NingshaProgressTextures.Rounded);
            }
            NingshaText.Label(label.ContractedBy(3f, 0f), value, NingshaPalette.Ink, GameFont.Tiny, TextAnchor.MiddleCenter);
        }
    }
}
