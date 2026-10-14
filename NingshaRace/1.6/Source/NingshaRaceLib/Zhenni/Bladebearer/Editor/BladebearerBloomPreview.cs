using System;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //使用 Pawn 相机当前投影和预览深度重绘辉光来源，与地图共用模糊合成器。
    internal sealed class BladebearerBloomPreview : IDisposable
    {
        private static BladebearerBloomPreview active;
        private readonly BladebearerBloomDraws draws = new BladebearerBloomDraws();
#if RW_1_7
        private BladebearerBloomPreviewRenderer nativeRenderer;
#else
        private BladebearerBloomProcessor processor;
#endif
        private RenderTexture target;
        private Exception error;

        //一次预览独占捕获上下文，普通头像和其他 Pawn 不受影响。
        internal void Begin(RenderTexture texture)
        {
            draws.Clear(); error = null; target = texture; active = this;
        }

        //沿用当前预览实际提交的图层、时钟和透明度。
        internal static void Record(PawnRenderNode_BladebearerPart node, Mesh mesh, Matrix4x4 matrix, Color tint)
        { active?.draws.Add(node, mesh, matrix, tint); }

#if RW_1_7
        //拦截原生烘焙入口，只有当前编辑器拥有的目标才使用选择性辉光。
        internal static bool RenderNative(RenderTexture target, System.Collections.Generic.IReadOnlyList<Waylith.Rendering.MeshDrawCommand> commands,
            Waylith.Rendering.CameraView view, Rect viewport, Color? background)
        {
            if (active == null || active.target != target || !active.draws.HasEmission) return false;
            try
            {
                active.nativeRenderer ??= new BladebearerBloomPreviewRenderer(BladebearerBloomCamera.ShaderDirectory);
                active.nativeRenderer.Render(target, commands, view, viewport, background, active.draws);
            }
            catch (Exception e) { active.error = e; }
            return true;
        }
#else
        //Unity 相机回调的异常交回窗口显示，不悄悄丢失辉光。
        internal static void RenderActive()
        {
            if (active == null || !active.draws.HasEmission) return;
            try { active.Render(); }
            catch (Exception e) { active.error = e; }
        }

        //相机仍保持绘制角色时的投影，此时共享目标深度可直接排除头盔与身体遮挡。
        private void Render()
        {
            RenderTexture emission = null, result = null;
            GL.PushMatrix();
            try
            {
                if (processor == null) processor = new BladebearerBloomProcessor();
                emission = RenderTexture.GetTemporary(target.width, target.height, 0, RenderTextureFormat.ARGBHalf);
                emission.filterMode = FilterMode.Bilinear; emission.wrapMode = TextureWrapMode.Clamp;
                result = RenderTexture.GetTemporary(target.width, target.height, 0, target.format);
                Graphics.SetRenderTarget(emission.colorBuffer, target.depthBuffer);
                GL.Clear(false, true, Color.clear);
                draws.Sort(Find.PawnCacheCamera);
                foreach (BladebearerBloomDraws.Draw draw in draws.Items)
                {
                    Material material = draw.CaptureMaterial(true);
                    draw.Apply(material);
                    if (!material.SetPass(draw.CapturePass)) throw new InvalidOperationException("无法使用震尼拥刀者辉光捕获 Shader。");
                    Graphics.DrawMeshNow(draw.Mesh, draw.Matrix);
                }
                processor.Render(target, emission, result);
                Graphics.Blit(result, target);
            }
            finally
            {
                Graphics.SetRenderTarget(target.colorBuffer, target.depthBuffer);
                GL.PopMatrix();
                if (emission != null) RenderTexture.ReleaseTemporary(emission);
                if (result != null) RenderTexture.ReleaseTemporary(result);
            }
        }

#endif
        //把回调中的错误送入现有窗口错误提示。
        internal void CheckError()
        { if (error != null) throw new InvalidOperationException("辉光预览失败：" + error.Message, error); }

        //无论预览成功与否都结束独占上下文。
        internal void End() { if (active == this) active = null; target = null; }

        //随窗口或预览尺寸变化释放资源。
        public void Dispose()
        {
            End(); draws.Clear(true);
#if RW_1_7
            nativeRenderer?.Dispose(); nativeRenderer = null;
#else
            processor?.Dispose(); processor = null;
#endif
        }
    }
}
