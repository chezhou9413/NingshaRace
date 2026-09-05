using UnityEngine;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：缓存可平铺的四尺度噪声，让风沙着色器通过少量纹理取样表现连续翻卷。
    internal static class NingshaStormNoise
    {
        private const int Size = 256;
        private static Texture2D texture;

        //职责：首次需要风沙时生成噪声，后续共用纹理且不保留可读的像素副本。
        public static Texture2D GetTexture()
        {
            if (texture != null) return texture;
            texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true)
            {
                name = "凝砂风沙四尺度噪声", hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size;
                    float v = (y + 0.5f) / Size;
                    //四通道分别承载大团沙尘、中型涡流、小型破碎与细碎消散形状。
                    pixels[y * Size + x] = new Color(Noise(u, v, 4, 17u), Noise(u, v, 8, 43u),
                        Noise(u, v, 16, 79u), Noise(u, v, 32, 127u));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        //职责：对周期格点作平滑插值，纹理两侧的数值与变化方向连续。
        private static float Noise(float u, float v, int cells, uint salt)
        {
            float px = u * cells;
            float py = v * cells;
            int x = Mathf.FloorToInt(px);
            int y = Mathf.FloorToInt(py);
            float tx = px - x;
            float ty = py - y;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float bottom = Mathf.Lerp(Hash(x % cells, y % cells, salt), Hash((x + 1) % cells, y % cells, salt), tx);
            float top = Mathf.Lerp(Hash(x % cells, (y + 1) % cells, salt), Hash((x + 1) % cells, (y + 1) % cells, salt), tx);
            return Mathf.Lerp(bottom, top, ty);
        }

        //职责：由格点与固定盐值计算噪声，不消耗游戏随机数或随帧改变分布。
        private static float Hash(int x, int y, uint salt)
        {
            unchecked
            {
                uint value = (uint)x * 374761393u + (uint)y * 668265263u + salt * 2246822519u;
                value = (value ^ (value >> 13)) * 1274126177u;
                value ^= value >> 16;
                return (value & 0x00ffffffu) / 16777215f;
            }
        }

        //职责：切换游戏时释放噪声纹理，使其与共用风沙材质一起回收。
        public static void Reset()
        {
            if (texture != null) Object.Destroy(texture);
            texture = null;
        }
    }
}
