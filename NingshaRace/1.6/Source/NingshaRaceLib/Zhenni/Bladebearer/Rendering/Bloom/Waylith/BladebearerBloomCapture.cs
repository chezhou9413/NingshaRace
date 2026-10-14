#if RW_1_7
using System;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Waylith.Graphics;
using Waylith.Rendering;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //清空发光颜色但加载已有场景深度，避免辉光穿过前景实体。
    internal sealed class BladebearerBloomCapture : IDisposable
    {
        private readonly DeviceEx device;
        private readonly MeshBatchRenderer renderer;
        private RenderTexture[] colors;
        private VkFramebuffer[] framebuffers;
        private VkImageView[] depthViews;
        private VkRenderPass pass;
        private VkFormat depthFormat;

        //地图与预览各自创建捕获器，保持描述符和网格缓存独立。
        internal BladebearerBloomCapture(GraphicsDevice graphics, IRenderContext context)
        { device = graphics.Device; renderer = new MeshBatchRenderer(graphics, context); }

        //每个在途帧独占发光目标，只在该帧的 GPU 栅栏完成后重用。
        internal RenderTexture Render(in FrameContext ctx, RenderTexture scene, MeshDrawFrame frame)
        {
            if (pass.IsNull())
            {
                depthFormat = scene.DepthFormat;
                pass = CreatePass(device, depthFormat);
                colors = new RenderTexture[ctx.RenderContext.FramesInFlight];
                framebuffers = new VkFramebuffer[colors.Length];
                depthViews = new VkImageView[colors.Length];
            }
            if (scene.DepthFormat != depthFormat) throw new InvalidOperationException("刀兵辉光深度格式发生变化，需重建渲染目标。");
            int slot = ctx.SubmissionFrameIndex;
            var color = colors[slot];
            if (color == null || color.Width != scene.Width || color.Height != scene.Height || !depthViews[slot].Equals(scene.DepthView))
            {
                if (!framebuffers[slot].IsNull()) device.DestroyFramebuffer(framebuffers[slot], device.VkAllocator);
                color?.Dispose();
                color = colors[slot] = new RenderTexture(scene.Width, scene.Height, VkFormat.R16G16B16A16SFloat, name: "Ningsha.Bloom.Emission");
                depthViews[slot] = scene.DepthView;
                framebuffers[slot] = CreateFramebuffer(color, scene.DepthView);
            }
            var emissionContext = new FrameContext(ctx.RenderContext, ctx.Resources, ctx.CommandBuffer, pass,
                framebuffers[slot], color.Extent, VkSampleCountFlags._1Bit, color.ColorFormat, scene.DepthFormat);
            Draw(emissionContext, renderer, frame, default);
            color.Color.SetMipLayout(0, VkImageLayout.ShaderReadOnlyOptimal);
            return color;
        }

        //场景与发光捕获使用同一投影和反向深度约定。
        internal static unsafe void Draw(in FrameContext ctx, MeshBatchRenderer meshRenderer, MeshDrawFrame frame, float4 clearColor)
        {
            meshRenderer.BeginFrame(ctx, frame.FrameNumber);
            VkClearValue* clears = stackalloc VkClearValue[2];
            clears[0] = new VkClearValue { Color = new VkClearColorValue { Float32 = clearColor } };
            clears[1] = new VkClearValue { DepthStencil = new VkClearDepthStencilValue { Depth = 0, Stencil = 0 } };
            ctx.CommandBuffer.BeginRenderPass(new VkRenderPassBeginInfo
            {
                RenderPass = ctx.RenderPass, Framebuffer = ctx.Framebuffer, RenderArea = new VkRect2D(ctx.Extent),
                ClearValueCount = 2, ClearValues = clears
            }, VkSubpassContents.Inline);
            var viewport = new VkViewport { Width = ctx.Extent.Width, Height = ctx.Extent.Height, MinDepth = 0, MaxDepth = 1 };
            var scissor = new VkRect2D(ctx.Extent);
            ctx.CommandBuffer.SetViewport(0, new ReadOnlySpan<VkViewport>(in viewport));
            ctx.CommandBuffer.SetScissor(0, new ReadOnlySpan<VkRect2D>(in scissor));
            meshRenderer.RecordBatch(ctx, frame.Draws, frame.View, frame.SunDirection);
            ctx.CommandBuffer.EndRenderPass();
        }

        //发光颜色与场景深度绑定到同一 framebuffer，深度所有权仍属于场景。
        private unsafe VkFramebuffer CreateFramebuffer(RenderTexture color, VkImageView depth)
        {
            VkImageView* attachments = stackalloc VkImageView[2] { color.ColorView, depth };
            return device.CreateFramebuffer(new VkFramebufferCreateInfo
            {
                RenderPass = pass, AttachmentCount = 2, Attachments = attachments,
                Width = color.Width, Height = color.Height, Layers = 1
            }, device.VkAllocator);
        }

        //读写屏障覆盖先前地图深度写入，以及后续模糊对颜色的采样。
        private static unsafe VkRenderPass CreatePass(DeviceEx device, VkFormat depthFormat)
        {
            VkAttachmentDescription* attachments = stackalloc VkAttachmentDescription[2];
            attachments[0] = new VkAttachmentDescription
            {
                Format = VkFormat.R16G16B16A16SFloat, Samples = VkSampleCountFlags._1Bit,
                LoadOp = VkAttachmentLoadOp.Clear, StoreOp = VkAttachmentStoreOp.Store,
                StencilLoadOp = VkAttachmentLoadOp.DontCare, StencilStoreOp = VkAttachmentStoreOp.DontCare,
                InitialLayout = VkImageLayout.Undefined, FinalLayout = VkImageLayout.ShaderReadOnlyOptimal
            };
            attachments[1] = new VkAttachmentDescription
            {
                Format = depthFormat, Samples = VkSampleCountFlags._1Bit,
                LoadOp = VkAttachmentLoadOp.Load, StoreOp = VkAttachmentStoreOp.Store,
                StencilLoadOp = VkAttachmentLoadOp.Load, StencilStoreOp = VkAttachmentStoreOp.Store,
                InitialLayout = VkImageLayout.DepthStencilAttachmentOptimal, FinalLayout = VkImageLayout.DepthStencilAttachmentOptimal
            };
            var color = new VkAttachmentReference { Attachment = 0, Layout = VkImageLayout.ColorAttachmentOptimal };
            var depth = new VkAttachmentReference { Attachment = 1, Layout = VkImageLayout.DepthStencilAttachmentOptimal };
            var subpass = new VkSubpassDescription
            { PipelineBindPoint = VkPipelineBindPoint.Graphics, ColorAttachmentCount = 1, ColorAttachments = &color, DepthStencilAttachment = &depth };
            VkSubpassDependency* dependencies = stackalloc VkSubpassDependency[2];
            dependencies[0] = new VkSubpassDependency
            {
                SrcSubpass = -1, DstSubpass = 0,
                SrcStageMask = VkPipelineStageFlags.FragmentShaderBit | VkPipelineStageFlags.EarlyFragmentTestsBit | VkPipelineStageFlags.LateFragmentTestsBit | VkPipelineStageFlags.ColorAttachmentOutputBit,
                DstStageMask = VkPipelineStageFlags.EarlyFragmentTestsBit | VkPipelineStageFlags.LateFragmentTestsBit | VkPipelineStageFlags.ColorAttachmentOutputBit,
                SrcAccessMask = VkAccessFlags.ShaderReadBit | VkAccessFlags.DepthStencilAttachmentWriteBit | VkAccessFlags.ColorAttachmentWriteBit,
                DstAccessMask = VkAccessFlags.DepthStencilAttachmentReadBit | VkAccessFlags.DepthStencilAttachmentWriteBit | VkAccessFlags.ColorAttachmentWriteBit
            };
            dependencies[1] = new VkSubpassDependency
            {
                SrcSubpass = 0, DstSubpass = -1,
                SrcStageMask = VkPipelineStageFlags.ColorAttachmentOutputBit | VkPipelineStageFlags.LateFragmentTestsBit,
                DstStageMask = VkPipelineStageFlags.FragmentShaderBit | VkPipelineStageFlags.EarlyFragmentTestsBit,
                SrcAccessMask = VkAccessFlags.ColorAttachmentWriteBit | VkAccessFlags.DepthStencilAttachmentWriteBit,
                DstAccessMask = VkAccessFlags.ShaderReadBit | VkAccessFlags.DepthStencilAttachmentReadBit
            };
            return device.CreateRenderPass(new VkRenderPassCreateInfo
            { AttachmentCount = 2, Attachments = attachments, SubpassCount = 1, Subpasses = &subpass, DependencyCount = 2, Dependencies = dependencies }, device.VkAllocator);
        }

        //引擎在重建窗口目标时保证旧帧已停止使用这些资源。
        internal void Rebuild(IRenderContext context)
        { ReleaseTargets(); renderer.OnRenderContextRebuilt(context); }

        //先解除深度附件引用，再释放发光目标和渲染通道。
        private void ReleaseTargets()
        {
            if (colors != null)
                for (int i = 0; i < colors.Length; i++)
                {
                    if (!framebuffers[i].IsNull()) device.DestroyFramebuffer(framebuffers[i], device.VkAllocator);
                    colors[i]?.Dispose();
                }
            colors = null; framebuffers = null; depthViews = null;
            if (!pass.IsNull()) device.DestroyRenderPass(pass, device.VkAllocator);
            pass = default;
        }

        //随所属渲染特性或预览窗口关闭。
        public void Dispose() { ReleaseTargets(); renderer.Dispose(); }
    }
}
#endif
