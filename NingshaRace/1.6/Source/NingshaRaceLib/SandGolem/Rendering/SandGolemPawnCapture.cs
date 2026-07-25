using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Rendering
{
    //类职责：把施法者当前四方向完整外观截图成沙傀运行时贴图。
    public static class SandGolemPawnCapture
    {
        //字段职责：控制沙傀截图纹理尺寸。
        private const int CaptureSize = 512;

        //字段职责：控制 Pawn 在截图里的缩放比例。
        private const float CameraZoom = 1.08f;

        //函数职责：捕获 Pawn 当前外观的四方向彩色贴图。
        public static Texture2D[] CapturePawn(Pawn pawn)
        {
            if (pawn == null || Find.PawnCacheRenderer == null)
            {
                return null;
            }

            Texture2D[] textures = new Texture2D[Rot4.RotationCount];
            foreach (Rot4 rotation in Rot4.AllRotations)
            {
                textures[rotation.AsInt] = CaptureRotation(pawn, rotation);
            }

            return textures;
        }

        //函数职责：捕获 Pawn 指定朝向的彩色贴图。
        private static Texture2D CaptureRotation(Pawn pawn, Rot4 rotation)
        {
            RenderTexture renderTexture = CreateRenderTexture();
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                Find.PawnCacheRenderer.RenderPawn(pawn, renderTexture, Vector3.zero, CameraZoom, 0f, rotation, renderHead: true, renderHeadgear: true, renderClothes: true);
                Texture2D texture = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGBA32, mipChain: false);
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0);
                texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.name = "NingshaRace_SandGolemCapture_" + pawn.thingIDNumber + "_" + rotation.AsInt;
                return texture;
            }
            finally
            {
                RenderTexture.active = previousActive;
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }

        //函数职责：创建用于透明背景 Pawn 截图的 RenderTexture。
        private static RenderTexture CreateRenderTexture()
        {
            RenderTexture renderTexture = new RenderTexture(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 4;
            renderTexture.Create();
            return renderTexture;
        }
    }
}
