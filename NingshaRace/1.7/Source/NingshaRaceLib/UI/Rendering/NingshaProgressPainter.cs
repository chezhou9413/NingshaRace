using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：绘制进度条的凹槽、渐变砂面和柔光，文字与交互由上层负责。
    internal static class NingshaProgressPainter
    {
        //职责：根据可用高度选择完整砂槽或紧凑细条，始终以真实比例限定填充。
        public static void Draw(Rect rect, float fraction, Color accent, float threshold)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 0f || rect.height <= 0f) return;
            using (new NingshaGuiScope(GameFont.Tiny))
            {
                bool compact = rect.height < 10f;
                Rect inner = compact ? rect : rect.ContractedBy(2f);
                if (inner.width <= 0f || inner.height <= 0f) return;
                if (compact) Widgets.DrawBoxSolid(rect, NingshaPalette.Recess);
                else
                {
                    GUI.color = Color.Lerp(NingshaPalette.Stone, NingshaPalette.Brass, 0.65f);
                    Widgets.DrawAtlas(rect, NingshaProgressTextures.Rounded);
                    GUI.color = NingshaPalette.Recess;
                    Widgets.DrawAtlas(rect.ContractedBy(1f), NingshaProgressTextures.Rounded);
                }
                Rect filled = inner;
                filled.width *= Mathf.Clamp01(fraction);
                if (filled.width > 0f) DrawFill(filled, accent, !compact && fraction < 1f);
                if (!compact) DrawMarkers(inner, threshold);
            }
        }

        //职责：在数值绘制后保留阈值两端的短标记，避免数字底板完全遮住关键界限。
        public static void DrawThresholdEnds(Rect rect, float threshold)
        {
            if (Event.current.type != EventType.Repaint || rect.height < 10f || rect.width < 8f
                || threshold <= 0f || threshold >= 1f) return;
            float x = rect.x + 2f + (rect.width - 4f) * threshold;
            x = Mathf.Clamp(x, rect.x + 2f, rect.xMax - 2f);
            Widgets.DrawBoxSolid(new Rect(x - 2f, rect.y, 4f, 2f), NingshaPalette.Jade);
            Widgets.DrawBoxSolid(new Rect(x - 2f, rect.yMax - 2f, 4f, 2f), NingshaPalette.Jade);
        }

        //职责：以连续渐变和柔和前缘替代硬色块，流光仅在尚未完成且有填充时移动。
        private static void DrawFill(Rect filled, Color accent, bool moving)
        {
            GUI.color = accent;
            GUI.DrawTextureWithTexCoords(filled, NingshaProgressTextures.Surface, new Rect(0f, 0f, 1f, 1f));
            float glowWidth = Mathf.Min(22f, filled.width);
            GUI.color = Color.Lerp(accent, NingshaPalette.Ink, 0.65f);
            GUI.DrawTexture(new Rect(filled.xMax - glowWidth, filled.y, glowWidth, filled.height), NingshaProgressTextures.LeadingGlow);
            if (!moving || filled.width < 4f) return;
            float bandWidth = Mathf.Clamp(filled.width * 0.35f, 26f, 88f);
            float phase = Mathf.Repeat(Time.realtimeSinceStartup * 0.18f, 1f);
            float x = filled.x - bandWidth + (filled.width + bandWidth) * phase;
            Rect band = new Rect(x, filled.y, bandWidth, filled.height);
            //根据交集换算纹理坐标，不依赖全局裁剪组，也不允许流光泄漏到未完成区域。
            float left = Mathf.Max(filled.x, band.x);
            float right = Mathf.Min(filled.xMax, band.xMax);
            if (right <= left) return;
            GUI.color = NingshaPalette.Ink;
            GUI.DrawTextureWithTexCoords(new Rect(left, band.y, right - left, band.height), NingshaProgressTextures.Shimmer,
                new Rect((left - band.x) / band.width, 0f, (right - left) / band.width, 1f));
        }

        //职责：以低对比四分刻度和独立阈值标记提供参照，避免密集刻线干扰数值阅读。
        private static void DrawMarkers(Rect inner, float threshold)
        {
            for (int i = 1; i < 4; i++)
                Widgets.DrawBoxSolid(new Rect(inner.x + inner.width * i / 4f, inner.yMax - 2f, 1f, 2f),
                    new Color(NingshaPalette.Ink.r, NingshaPalette.Ink.g, NingshaPalette.Ink.b, 0.25f));
            if (threshold <= 0f || threshold >= 1f || inner.width < 4f) return;
            float x = Mathf.Clamp(inner.x + inner.width * threshold, inner.x + 1.5f, inner.xMax - 1.5f);
            Widgets.DrawBoxSolid(new Rect(x - 0.5f, inner.y + 1f, 1f, inner.height - 2f), NingshaPalette.Jade);
            Widgets.DrawBoxSolid(new Rect(x - 1.5f, inner.y, 3f, 2f), NingshaPalette.Jade);
        }
    }
}
