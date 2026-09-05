using System;
using ChezhouLib.ALLmap;
using UnityEngine;

namespace NingshaRaceLib.UI.Rendering
{
    //类职责：通过 CL 加载风沙着色器，每帧最多生成一次所有面板共用的完整背景。
    internal static class NingshaDriftSurface
    {
        public const int Width = 512;
        public const int Height = 512;
        private static readonly int FlowTimeId = Shader.PropertyToID("_FlowTime");
        private static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");
        private static readonly int GrainTexId = Shader.PropertyToID("_GrainTex");
        private static Material material;
        private static RenderTexture surface;
        private static int renderedFrame = -1;

        //职责：按需建立画面并传递实时时间，同一引擎帧内的后续面板直接复用结果。
        public static Texture GetCurrentTexture()
        {
            if (surface == null) CreateResources();
            if (renderedFrame == Time.frameCount) return surface;
            RenderTexture previous = RenderTexture.active;
            try
            {
                material.SetFloat(FlowTimeId, Time.realtimeSinceStartup);
                Graphics.Blit(Texture2D.whiteTexture, surface, material);
                renderedFrame = Time.frameCount;
            }
            finally
            {
                RenderTexture.active = previous;
            }
            return surface;
        }

        //职责：创建唯一风沙材质和无深度全幅纹理，绑定噪声与自由细沙，资源失败时直接报告错误。
        private static void CreateResources()
        {
            Shader shader = abDatabase.GetShader("Ningsha/UI/DriftingSand", "chezhou.race.ningsharace");
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("凝砂 UI 缺少或无法使用 DriftingSand Shader，请检查 ningsha_ui.ab 和 UnityShaderLord 声明。");
            material = new Material(shader)
            {
                name = "凝砂界面全幅风沙材质", hideFlags = HideFlags.HideAndDontSave
            };
            material.SetTexture(NoiseTexId, NingshaStormNoise.GetTexture());
            material.SetTexture(GrainTexId, NingshaSandGrainTexture.GetTexture());
            surface = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32)
            {
                name = "凝砂界面共用风沙背景", hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                useMipMap = false, autoGenerateMips = false
            };
            if (!surface.Create())
            {
                Reset();
                throw new InvalidOperationException("凝砂界面流沙纹理创建失败。");
            }
        }

        //职责：释放背景、噪声、细沙与材质并清除帧标记，使后续游戏能够重新建立界面资源。
        public static void Reset()
        {
            if (surface != null)
            {
                surface.Release();
                UnityEngine.Object.Destroy(surface);
                surface = null;
            }
            if (material != null) UnityEngine.Object.Destroy(material);
            material = null;
            NingshaStormNoise.Reset();
            NingshaSandGrainTexture.Reset();
            renderedFrame = -1;
        }
    }
}
