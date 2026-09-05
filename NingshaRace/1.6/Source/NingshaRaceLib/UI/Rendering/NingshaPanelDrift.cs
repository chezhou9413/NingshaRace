using UnityEngine;
using Verse;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：把连续风沙铺满窗口、状态卡和按钮的整个底板，不改变前景内容与交互区域。
    internal static class NingshaPanelDrift
    {
        //职责：只在重绘时取用流沙画面，通过标准界面纹理绘制保留滚动裁剪和颜色状态。
        public static void Draw(Rect rect, float hover, bool inset)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 1f || rect.height <= 1f) return;
            float strength = inset ? 0.46f : Mathf.Lerp(0.88f, 1f, Mathf.Clamp01(hover));
            if (!GUI.enabled) strength *= 0.6f;

            Color previous = GUI.color;
            try
            {
                Texture surface = NingshaDriftSurface.GetCurrentTexture();
                GUI.color = previous * new Color(1f, 1f, 1f, strength);
                //以左上角为取样锚点，按界面像素平铺，拖动或拉大面板不会拉伸沙粒。
                float uvWidth = rect.width / NingshaDriftSurface.Width;
                float uvHeight = rect.height / NingshaDriftSurface.Height;
                GUI.DrawTextureWithTexCoords(rect, surface, new Rect(0f, 1f - uvHeight, uvWidth, uvHeight));
            }
            finally
            {
                GUI.color = previous;
            }
        }

        //职责：在游戏切换时释放流沙画面及其材质，由统一图形生命周期调用。
        public static void Reset()
        {
            NingshaDriftSurface.Reset();
        }
    }
}
