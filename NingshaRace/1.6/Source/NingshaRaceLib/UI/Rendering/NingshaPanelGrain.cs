using UnityEngine;
using Verse;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：缓存并绘制面板的细沙颗粒与边缘积沙，不参与文字布局或游戏随机数计算。
    internal static class NingshaPanelGrain
    {
        private const int TileSize = 128;
        private const int EdgeSize = 16;
        private static readonly Color LightSand = new Color(0.83f, 0.69f, 0.46f);
        private static readonly Color DarkSand = new Color(0.025f, 0.019f, 0.012f);
        private static Texture2D surface;
        private static Texture2D horizontalEdge;
        private static Texture2D verticalEdge;

        //职责：在底材之后、内容之前叠加静止颗粒，紧凑或凹入面板降低对比度。
        public static void Draw(Rect rect, bool inset)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 0f || rect.height <= 0f) return;
            Color previous = GUI.color;
            try
            {
                bool compact = Mathf.Min(rect.width, rect.height) < 56f;
                float strength = inset ? 0.48f : compact ? 0.58f : 0.76f;
                GUI.color = new Color(1f, 1f, 1f, strength);
                if (surface == null) surface = CreateTexture(TileSize, TileSize, false, false);
                GUI.DrawTextureWithTexCoords(rect, surface,
                    new Rect(0f, 0f, rect.width / TileSize, rect.height / TileSize));
                if (!compact) DrawEdges(rect, strength * 0.65f);
            }
            finally
            {
                GUI.color = previous;
            }
        }

        //职责：用向内渐淡的边带表现积沙，角部自然叠加，保持中央说明区域较安静。
        private static void DrawEdges(Rect rect, float strength)
        {
            if (horizontalEdge == null) horizontalEdge = CreateTexture(TileSize, EdgeSize, true, false);
            if (verticalEdge == null) verticalEdge = CreateTexture(EdgeSize, TileSize, false, true);
            GUI.color = new Color(1f, 1f, 1f, strength);
            float width = rect.width / TileSize;
            float height = rect.height / TileSize;
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, rect.width, EdgeSize),
                horizontalEdge, new Rect(0f, 0f, width, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.yMax - EdgeSize, rect.width, EdgeSize),
                horizontalEdge, new Rect(0f, 1f, width, -1f));
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, EdgeSize, rect.height),
                verticalEdge, new Rect(0f, 0f, 1f, height));
            GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - EdgeSize, rect.y, EdgeSize, rect.height),
                verticalEdge, new Rect(1f, 0f, -1f, height));
        }

        //职责：一次生成可平铺的细沙或单向渐隐边带，上传后释放像素副本。
        private static Texture2D CreateTexture(int width, int height, bool horizontal, bool vertical)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = horizontal ? "凝砂横向积沙" : vertical ? "凝砂纵向积沙" : "凝砂面板细沙",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapModeU = vertical ? TextureWrapMode.Clamp : TextureWrapMode.Repeat,
                wrapModeV = horizontal ? TextureWrapMode.Clamp : TextureWrapMode.Repeat
            };
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = GrainPixel(x, y);
                    //横带的上沿与纵带的左沿为外缘，另一侧平滑收至完全透明。
                    float inward = horizontal ? (height - 1f - y) / (height - 1f)
                        : vertical ? x / (width - 1f) : 0f;
                    color.a *= 1f - Mathf.SmoothStep(0f, 1f, inward);
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        //职责：以微尘和疏密不一的椭圆沙粒组成底纹，亮面与暗面共同表现细小凹凸。
        private static Color GrainPixel(int x, int y)
        {
            float dust = Hash(x, y, 37);
            Color color = dust > 0.54f ? LightSand : DarkSand;
            color.a = 0.025f + Mathf.Abs(dust - 0.5f) * 0.075f;
            int cellX = x / 4;
            int cellY = y / 4;
            if (Hash(cellX, cellY, 11) < 0.36f) return color;

            //沙粒中心带有偏移且远离单元边界，平铺时不切断颗粒，也不形成规则点阵。
            float dx = (x % 4 + 0.5f - 1.25f - Hash(cellX, cellY, 23) * 1.5f)
                / (0.65f + Hash(cellX, cellY, 41) * 0.48f);
            float dy = (y % 4 + 0.5f - 1.25f - Hash(cellX, cellY, 59) * 1.5f)
                / (0.55f + Hash(cellX, cellY, 71) * 0.42f);
            float coverage = Mathf.Clamp01((1f - dx * dx - dy * dy) * 1.7f);
            if (coverage <= 0f) return color;
            float light = Mathf.Clamp01(0.5f + (dy - dx) * 0.48f);
            Color grain = Color.Lerp(DarkSand, LightSand, light);
            grain.a = 0.10f + Mathf.Abs(light - 0.5f) * 0.22f;
            return Color.Lerp(color, grain, coverage);
        }

        //职责：从坐标生成固定噪声，使重绘、暂停和重新打开界面时的沙粒保持稳定。
        private static float Hash(int x, int y, uint salt)
        {
            unchecked
            {
                uint value = (uint)x * 374761393u + (uint)y * 668265263u + salt * 1442695041u;
                value = (value ^ (value >> 13)) * 1274126177u;
                value ^= value >> 16;
                return (value & 0x00ffffffu) / 16777215f;
            }
        }

        //职责：切换游戏时释放共享颗粒纹理，下一次面板重绘时按需创建。
        public static void Reset()
        {
            Object.Destroy(surface);
            Object.Destroy(horizontalEdge);
            Object.Destroy(verticalEdge);
            surface = horizontalEdge = verticalEdge = null;
        }
    }
}
