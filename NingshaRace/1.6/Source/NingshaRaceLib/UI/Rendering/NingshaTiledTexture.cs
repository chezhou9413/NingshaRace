using UnityEngine;

namespace NingshaRaceLib.UI.Rendering
{
    //把重复纹理切成单张范围内的矩形，避开宿主普通 GUI 的固定边缘钳制采样。
    internal static class NingshaTiledTexture
    {
        //保留原始 UV 比例、相位和翻转，裁剪与颜色仍由 GUI 处理。
        internal static void Draw(Rect rect, Texture texture, Rect uv)
        {
#if RW_1_7
            if (rect.width <= 0f || rect.height <= 0f || uv.width == 0f || uv.height == 0f) return;
            float uMin = Mathf.Min(uv.x, uv.x + uv.width);
            float uMax = Mathf.Max(uv.x, uv.x + uv.width);
            float vMin = Mathf.Min(uv.y, uv.y + uv.height);
            float vMax = Mathf.Max(uv.y, uv.y + uv.height);
            int xEnd = Mathf.CeilToInt(uMax), yEnd = Mathf.CeilToInt(vMax);
            for (int y = Mathf.FloorToInt(vMin); y < yEnd; y++)
            {
                float bottom = Mathf.Max(vMin, y), top = Mathf.Min(vMax, y + 1f);
                float t0 = (bottom - uv.y) / uv.height, t1 = (top - uv.y) / uv.height;
                float drawTop = rect.yMax - Mathf.Max(t0, t1) * rect.height;
                float drawBottom = rect.yMax - Mathf.Min(t0, t1) * rect.height;
                for (int x = Mathf.FloorToInt(uMin); x < xEnd; x++)
                {
                    float left = Mathf.Max(uMin, x), right = Mathf.Min(uMax, x + 1f);
                    float s0 = (left - uv.x) / uv.width, s1 = (right - uv.x) / uv.width;
                    float drawLeft = rect.x + Mathf.Min(s0, s1) * rect.width;
                    float drawRight = rect.x + Mathf.Max(s0, s1) * rect.width;
                    var tile = new Rect(drawLeft, drawTop, drawRight - drawLeft, drawBottom - drawTop);
                    var source = new Rect((uv.width > 0f ? left : right) - x,
                        (uv.height > 0f ? bottom : top) - y,
                        Mathf.Sign(uv.width) * (right - left), Mathf.Sign(uv.height) * (top - bottom));
                    GUI.DrawTextureWithTexCoords(tile, texture, source);
                }
            }
#else
            GUI.DrawTextureWithTexCoords(rect, texture, uv);
#endif
        }
    }
}
