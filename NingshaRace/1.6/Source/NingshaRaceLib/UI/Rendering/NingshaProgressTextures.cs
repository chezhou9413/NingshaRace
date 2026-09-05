using System;
using UnityEngine;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：缓存进度条共用的柔边、沉积渐变和流光纹理，不在逐帧绘制时分配图片。
    internal static class NingshaProgressTextures
    {
        private static Texture2D rounded;
        private static Texture2D surface;
        private static Texture2D shimmer;
        private static Texture2D leadingGlow;

        //属性职责：按需提供共用纹理，调用者只在界面重绘阶段访问。
        public static Texture2D Rounded => rounded ?? (rounded = Create("凝砂进度柔边", 16, 16, RoundPixel));
        public static Texture2D Surface => surface ?? (surface = Create("凝砂进度砂面", 256, 32, SurfacePixel));
        public static Texture2D Shimmer => shimmer ?? (shimmer = Create("凝砂进度流光", 128, 32, ShimmerPixel));
        public static Texture2D LeadingGlow => leadingGlow ?? (leadingGlow = Create("凝砂进度前缘", 64, 16, GlowPixel));

        //职责：创建不带额外缩小层级的双线性纹理，上传后释放不再需要的像素副本。
        private static Texture2D Create(string name, int width, int height, Func<float, float, Color> pixel)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name, hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            Color32[] colors = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    colors[y * width + x] = pixel((x + 0.5f) / width, (y + 0.5f) / height);
            texture.SetPixels32(colors);
            texture.Apply(false, true);
            return texture;
        }

        //职责：为九宫格边缘生成带一个像素柔化的圆角遮罩。
        private static Color RoundPixel(float x, float y)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(x * 16f - 8f) - 4.5f);
            float dy = Mathf.Max(0f, Mathf.Abs(y * 16f - 8f) - 4.5f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(4f - Mathf.Sqrt(dx * dx + dy * dy)));
        }

        //职责：生成上亮下暗的砂面与低对比颗粒，不消耗游戏随机数。
        private static Color SurfacePixel(float x, float y)
        {
            float grain = Mathf.Sin(x * 1841f + y * 723f) * Mathf.Sin(x * 977f - y * 391f) * 0.025f;
            float light = 0.55f + 0.28f * y + 0.13f * Mathf.Exp(-Mathf.Pow((y - 0.82f) * 9f, 2f)) + grain;
            float alpha = Mathf.Clamp01(Mathf.Min(y, 1f - y) * 32f);
            return new Color(light, light, light, alpha);
        }

        //职责：生成两侧渐隐且略有倾斜的柔光带，避免硬竖线反复横扫。
        private static Color ShimmerPixel(float x, float y)
        {
            float distance = (x - 0.5f + (y - 0.5f) * 0.16f) * 5.5f;
            float alpha = Mathf.Exp(-distance * distance) * Mathf.Sin(y * Mathf.PI) * 0.24f;
            return new Color(1f, 1f, 1f, alpha);
        }

        //职责：生成仅向已完成一侧扩散的前缘光晕，避免越过真实进度。
        private static Color GlowPixel(float x, float y)
        {
            return new Color(1f, 1f, 1f, x * x * x * Mathf.Sin(y * Mathf.PI) * 0.45f);
        }

        //职责：切换游戏时释放所有共用进度纹理，下一次绘制可重新建立。
        public static void Reset()
        {
            UnityEngine.Object.Destroy(rounded);
            UnityEngine.Object.Destroy(surface);
            UnityEngine.Object.Destroy(shimmer);
            UnityEngine.Object.Destroy(leadingGlow);
            rounded = surface = shimmer = leadingGlow = null;
        }
    }
}
