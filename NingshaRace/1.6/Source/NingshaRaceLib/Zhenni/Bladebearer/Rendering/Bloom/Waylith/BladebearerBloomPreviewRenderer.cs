#if RW_1_7
using System;
using System.Collections.Generic;
using System.IO;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Waylith;
using Waylith.Graphics;
using Waylith.Rendering;
using Waylith.Rendering.PostFx;
using Color = UnityEngine.Color;
using Rect = UnityEngine.Rect;
using UnityTexture = UnityEngine.RenderTexture;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //为编辑器保留完整角色深度，同步提交离屏渲染后交回 IMGUI。
    internal sealed class BladebearerBloomPreviewRenderer : IDisposable
    {
        private readonly EngineApplication app;
        private readonly GraphicsDevice graphics;
        private readonly DeviceEx device;
        private readonly BladebearerPreviewContext context;
        private readonly MeshBatchRenderer sceneRenderer;
        private readonly BladebearerBloomCapture capture;
        private readonly BladebearerBloomFilter filter;
        private readonly PostEffectPass composite;
        private readonly VkCommandPool pool;
        private readonly CommandBuffer command;
        private readonly VkFence fence;
        private RenderTexture scene, result;
        private VkRenderPass scenePass;
        private int frameNumber;
        private bool disposed;

        //独立命令池与栅栏避免和正在提交的地图帧共用录制状态。
        internal BladebearerBloomPreviewRenderer(string directory)
        {
            app = EngineApplication.Instance;
            graphics = app.GraphicsDevice; device = graphics.Device;
            pool = device.CreateCommandPool(new VkCommandPoolCreateInfo
            { QueueFamilyIndex = graphics.GraphicsFamilyIndex, Flags = VkCommandPoolCreateFlags.ResetCommandBufferBit }, device.VkAllocator);
            command = device.AllocateCommandBuffer(new VkCommandBufferAllocateInfo { CommandPool = pool, Level = VkCommandBufferLevel.Primary });
            fence = device.CreateFence(new VkFenceCreateInfo(), device.VkAllocator);
            context = new BladebearerPreviewContext(graphics, pool);
            sceneRenderer = new MeshBatchRenderer(graphics, context);
            capture = new BladebearerBloomCapture(graphics, context);
            filter = new BladebearerBloomFilter(directory);
            composite = new PostEffectPass("Ningsha.Bloom.Preview", Path.Combine(directory, "BloomComposite.frag.glsl"), new[] { 0, 1 });
            app.BeforeGraphicsShutdown += Shutdown;
        }

        //仅供刀兵编辑器全尺寸、纯色背景的 Pawn 相机预览调用。
        internal void Render(UnityTexture target, IReadOnlyList<MeshDrawCommand> commands, CameraView view,
            Rect viewport, Color? background, BladebearerBloomDraws sources)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BladebearerBloomPreviewRenderer));
            if (viewport != new Rect(0, 0, 1, 1) || !background.HasValue)
                throw new InvalidOperationException("刀兵辉光预览需要全尺寸视口和明确的背景颜色。");
            if (!target.Create() || target.GpuTexture == null)
                throw new InvalidOperationException("无法创建刀兵预览目标纹理。");
            GpuTextureService.FlushPendingTextureCommands();
            graphics.MeshUploads.DrainPendingUploads();
            EnsureTargets(target);
            int frame = ++frameNumber;
            using var sceneFrame = BladebearerBloomSnapshot.CaptureScene(commands, view, frame);
            using var emissionFrame = BladebearerBloomSnapshot.Capture(sources, Verse.Find.PawnCacheCamera, view, frame);
            var resources = context.FrameResources[context.FrameIndex];
            var ctx = new FrameContext(context, resources, command, scenePass, scene.GetFramebuffer(scenePass),
                scene.Extent, VkSampleCountFlags._1Bit, scene.ColorFormat, scene.DepthFormat);
            command.Begin(VkCommandBufferUsageFlags.OneTimeSubmitBit);
            var color = background.Value;
            BladebearerBloomCapture.Draw(ctx, sceneRenderer, sceneFrame, new float4(color.r, color.g, color.b, color.a));
            scene.Color.SetMipLayout(0, VkImageLayout.ShaderReadOnlyOptimal);
            var emission = capture.Render(ctx, scene, emissionFrame);
            var glow = filter.Render(ctx, emission);
            composite.SetInput(0, scene); composite.SetInput(1, glow); composite.Output = result;
            composite.Record(ctx, null);
            BladebearerBloomTransfer.Copy(command, result.Color, target.GpuTexture, target.width, target.height);
            command.End();
            device.ResetFences(new ReadOnlySpan<VkFence>(in fence));
            var submit = command;
            graphics.SubmitGraphics(default, default, new Span<CommandBuffer>(ref submit), default, fence);
            device.WaitForFence(fence, -1);
        }

        //场景深度必须保存到捕获阶段，原版头像烘焙器会丢弃该深度。
        private void EnsureTargets(UnityTexture target)
        {
            if (scene != null && scene.Width == target.width && scene.Height == target.height) return;
            capture.Rebuild(context);
            scene?.Dispose(); result?.Dispose();
            if (scenePass.IsNull()) scenePass = PostFxPassHelpers.CreateColorRenderPass(device,
                target.GpuTexture.Format, VkFormat.D32SFloatS8UInt, VkAttachmentLoadOp.Clear);
            scene = new RenderTexture(target.width, target.height, target.GpuTexture.Format, VkFormat.D32SFloatS8UInt, name: "Ningsha.Preview.Scene");
            result = new RenderTexture(target.width, target.height, target.GpuTexture.Format, name: "Ningsha.Preview.Composite");
            context.SetTarget(scene, scenePass);
        }

        //图形设备关闭前解除对原生资源的引用。
        private void Shutdown(EngineApplication application) => Dispose();

        //每次预览提交都已等待栅栏，窗口关闭时可直接释放。
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            app.BeforeGraphicsShutdown -= Shutdown;
            capture.Dispose(); filter.Dispose(); composite.Dispose(); sceneRenderer.Dispose();
            scene?.Dispose(); result?.Dispose();
            if (!scenePass.IsNull()) device.DestroyRenderPass(scenePass, device.VkAllocator);
            device.DestroyFence(fence, device.VkAllocator);
            device.DestroyCommandPool(pool, device.VkAllocator);
        }
    }
}
#endif
