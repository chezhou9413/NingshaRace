using UnityEngine;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：缓存自由散布的细沙形状，避免固定格子、统一大小与一致朝向形成排列感。
    internal static class NingshaSandGrainTexture
    {
        private const int Size = 512;
        private static Texture2D texture;

        //职责：首次绘制时生成独立的细沙、少量稍大沙粒与微尘通道，上传后释放像素副本。
        public static Texture2D GetTexture()
        {
            if (texture != null) return texture;
            Color32[] pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, (byte)(RandomValue(i, 97u) * 255f), 255);
            Scatter(pixels, 23000, 0, 0.28f, 0.70f);
            Scatter(pixels, 3500, 1, 0.60f, 1.15f);
            texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true)
            {
                name = "凝砂自由散布细沙", hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        //职责：让每颗沙粒在整张纹理内独立选取位置、半径、长短轴、方向和透明度。
        private static void Scatter(Color32[] pixels, int count, int channel, float minRadius, float maxRadius)
        {
            for (int i = 0; i < count; i++)
            {
                int seed = i + channel * 32771;
                float x = RandomValue(seed, 3u) * Size;
                float y = RandomValue(seed, 7u) * Size;
                float radius = Mathf.Lerp(minRadius, maxRadius, RandomValue(seed, 11u));
                float aspect = Mathf.Lerp(0.65f, 1.35f, RandomValue(seed, 17u));
                float angle = RandomValue(seed, 23u) * Mathf.PI * 2f;
                float opacity = Mathf.Lerp(0.35f, 0.95f, RandomValue(seed, 31u));
                Stamp(pixels, channel, x, y, radius, aspect, angle, opacity);
            }
        }

        //职责：以亚像素柔边绘制不规则椭圆，允许自然重叠，并把跨边界的部分回绕到另一侧。
        private static void Stamp(Color32[] pixels, int channel, float x, float y,
            float radius, float aspect, float angle, float opacity)
        {
            float major = radius * aspect + 0.35f;
            float minor = radius + 0.35f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            int extent = Mathf.CeilToInt(Mathf.Max(major, minor) * 1.8f);
            int centerX = Mathf.FloorToInt(x);
            int centerY = Mathf.FloorToInt(y);
            //亚像素颗粒扩宽取样支撑后按半径降低强度，避免变成一片等亮的硬像素。
            float strength = opacity * radius / (radius + 0.35f);
            for (int py = centerY - extent; py <= centerY + extent; py++)
            {
                for (int px = centerX - extent; px <= centerX + extent; px++)
                {
                    float dx = px + 0.5f - x;
                    float dy = py + 0.5f - y;
                    float u = (dx * cos + dy * sin) / major;
                    float v = (-dx * sin + dy * cos) / minor;
                    float distance = u * u + v * v;
                    if (distance > 3.2f) continue;
                    float alpha = Mathf.Exp(-distance * 1.65f) * strength;
                    int index = ((py + Size) % Size) * Size + (px + Size) % Size;
                    Color32 pixel = pixels[index];
                    byte old = channel == 0 ? pixel.r : pixel.g;
                    byte combined = (byte)Mathf.RoundToInt(old + (255 - old) * alpha);
                    if (channel == 0) pixel.r = combined;
                    else pixel.g = combined;
                    pixels[index] = pixel;
                }
            }
        }

        //职责：使用固定散列给不同参数提供独立随机值，不消耗游戏随机数也不逐帧重排。
        private static float RandomValue(int index, uint salt)
        {
            unchecked
            {
                uint value = (uint)(index + 1) * 374761393u + salt * 668265263u;
                value = (value ^ (value >> 13)) * 1274126177u;
                value ^= value >> 16;
                return (value & 0x00ffffffu) / 16777216f;
            }
        }

        //职责：切换游戏时释放共用细沙纹理，与风沙背景和噪声保持一致的生命周期。
        public static void Reset()
        {
            if (texture != null) Object.Destroy(texture);
            texture = null;
        }
    }
}
