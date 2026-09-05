using System;
using ChezhouLib.ALLmap;
using UnityEngine;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：通过 CL 取得砂岩 Shader 并缓存界面底纹，避免每个控件重复运行离屏渲染。
    public static class NingshaUiAssets
    {
        private static Material stoneMaterial;
        private static RenderTexture stoneTexture;

        //属性职责：在首次绘制时生成共用砂岩底纹，后续使用标准 GUI 纹理绘制保留裁剪行为。
        public static Texture Stone
        {
            get
            {
                if (stoneTexture == null) CreateStone();
                return stoneTexture;
            }
        }

        //函数职责：创建唯一离屏底纹并完整恢复渲染目标，不把专用 Shader 直接用于滚动区绘制。
        private static void CreateStone()
        {
            Shader shader = abDatabase.GetShader("Ningsha/UI/WeatheredSandstone", "chezhou.race.ningsharace");
            if (shader == null) throw new InvalidOperationException("凝砂 UI 缺少 WeatheredSandstone Shader，请检查 ningsha_ui.ab 和 UnityShaderLord 声明。");
            stoneMaterial = new Material(shader) { name = "凝砂界面砂岩材质", hideFlags = HideFlags.HideAndDontSave };
            stoneTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32)
            {
                name = "凝砂界面砂岩底纹", hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear
            };
            RenderTexture previous = RenderTexture.active;
            try
            {
                stoneTexture.Create();
                Graphics.Blit(Texture2D.whiteTexture, stoneTexture, stoneMaterial);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        //函数职责：释放当前游戏使用的 GPU 材质和底纹，允许后续游戏重新创建。
        public static void Reset()
        {
            if (stoneTexture != null)
            {
                stoneTexture.Release();
                UnityEngine.Object.Destroy(stoneTexture);
                stoneTexture = null;
            }
            if (stoneMaterial != null) UnityEngine.Object.Destroy(stoneMaterial);
            stoneMaterial = null;
        }
    }
}
