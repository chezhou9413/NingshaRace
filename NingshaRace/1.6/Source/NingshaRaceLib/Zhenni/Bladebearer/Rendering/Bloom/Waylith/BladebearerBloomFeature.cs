#if RW_1_7
using System;
using System.Collections.Generic;
using System.IO;
using RimWorldWaylith.Rendering;
using UnityStubs.Rendering;
using Verse;
using Waylith;
using Waylith.Graphics;
using Waylith.Rendering;
using Waylith.Rendering.PostFx;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //在离屏阶段提取发光网格，窗口阶段仅把光晕叠加到已合成的地图上。
    internal sealed class BladebearerBloomFeature : IRenderFeature, IDisposable
    {
        private readonly EngineApplication app;
        private readonly MapRenderFeature map;
        private readonly SceneColorTargets scene;
        private readonly BladebearerBloomCapture capture;
        private readonly BladebearerBloomFilter filter;
        private readonly PostEffectPass overlay;
        public bool IsActive { get; private set; }
        public IReadOnlyList<IRenderPass> Passes { get; }

        //由 FrameRenderer 管理帧快照、重建回调和销毁时机。
        internal BladebearerBloomFeature(EngineApplication app, MapRenderFeature map, SceneColorTargets scene, string directory)
        {
            this.app = app; this.map = map; this.scene = scene;
            capture = new BladebearerBloomCapture(app.GraphicsDevice, app.RenderContext);
            filter = new BladebearerBloomFilter(directory);
            overlay = new PostEffectPass("Ningsha.Bloom.Overlay", Path.Combine(directory, "BloomOverlay.frag.glsl"), new[] { 0 },
                new BlendState(true, BlendFactor.OneMinusDstColor, BlendFactor.One, BlendOp.Add, BlendFactor.Zero, BlendFactor.One, BlendOp.Add));
            Passes = new IRenderPass[] { new BloomPass(this, true), new BloomPass(this, false) };
        }

        //此入口由主线程调用，之后不再读取 Pawn、地图或可变部件属性。
        public object CaptureFrame(in FrameTiming timing)
        {
            if (Find.CurrentMap == null) BladebearerBloomSources.Clear();
            var sources = BladebearerBloomSources.Current;
            IsActive = map.IsActive && sources != null && sources.HasEmission;
            if (!IsActive) return null;
            var camera = Find.Camera;
            return BladebearerBloomSnapshot.Capture(sources, camera, CameraViewFactory.GetView(camera), timing.FrameNumber);
        }

        //地图深度保存在当前提交帧的场景目标中，不能使用前一帧的附件。
        private void Record(in FrameContext ctx, object payload, bool preBackbuffer)
        {
            if (!(payload is MeshDrawFrame frame)) return;
            if (preBackbuffer)
            {
                var emission = capture.Render(ctx, scene.Current(ctx.SubmissionFrameIndex), frame);
                overlay.SetInput(0, filter.Render(ctx, emission));
            }
            else overlay.RecordInline(ctx, ctx.RenderPass, ctx.SampleCount);
        }

        //窗口目标重建后丢弃依赖旧 render pass 的管线。
        public void OnRenderTargetRebuilt(IRenderTarget target)
        { capture.Rebuild(app.RenderContext); filter.Rebuild(); overlay.Invalidate(); }

        //引擎等待在途帧结束后调用，避免释放 GPU 仍在使用的附件。
        public void Dispose() { capture.Dispose(); filter.Dispose(); overlay.Dispose(); }

        //两个固定阶段共用一份帧快照，资源生命周期归所属特性管理。
        private sealed class BloomPass : IRenderPass
        {
            private readonly BladebearerBloomFeature owner;
            public byte PassId { get; }
            public PassAttachment Attachment => null;
            public bool PreBackbuffer { get; }

            //把深度捕获和最终叠加分到引擎规定的两个阶段。
            internal BloomPass(BladebearerBloomFeature owner, bool preBackbuffer)
            {
                this.owner = owner; PreBackbuffer = preBackbuffer;
                PassId = RenderPassRegistry.Register(preBackbuffer ? "Ningsha.Bloom.Capture" : "Ningsha.Bloom.Composite");
            }
            //仅接收当前渲染帧的不可变载荷。
            public void Record(in FrameContext ctx, object payload) => owner.Record(ctx, payload, PreBackbuffer);
            //所属特性统一处理重建，避免重复释放。
            public void OnRenderTargetRebuilt(IRenderTarget target) { }
            //所属特性统一持有资源。
            public void Dispose() { }
        }
    }
}
#endif
