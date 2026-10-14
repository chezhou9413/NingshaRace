#if RW_1_7
using System;
using System.IO;
using System.Numerics;
using Brutal.VulkanApi;
using Waylith.Rendering;
using Waylith.Rendering.PostFx;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //四级半精度辉光金字塔，每次记录分配独立描述符，直到整帧完成才复用。
    internal sealed class BladebearerBloomFilter : PostEffectPass
    {
        private const int Levels = 4;
        private RenderTexture[][] textures;

        //地图和预览各持有独立实例，原生着色器文件随模组部署。
        internal BladebearerBloomFilter(string directory) : base("Ningsha.Bloom.Filter",
            Path.Combine(directory, "BloomFilter.frag.glsl"), new[] { 0, 1 }, pushConstantSize: 16, maxPassesPerFrame: 15) { }

        //降采样后做横纵高斯模糊，再逐级混入较宽的光晕。
        internal RenderTexture Render(in FrameContext ctx, RenderTexture emission)
        {
            if (textures == null) textures = new RenderTexture[ctx.RenderContext.FramesInFlight][];
            int frame = ctx.SubmissionFrameIndex;
            var pyramid = textures[frame] ??= new RenderTexture[Levels * 2];
            int width = emission.Width, height = emission.Height;
            for (int i = 0; i < Levels; i++)
            {
                width = Math.Max(1, width / 2); height = Math.Max(1, height / 2);
                for (int j = 0; j < 2; j++)
                {
                    int slot = i * 2 + j;
                    pyramid[slot] ??= new RenderTexture(width, height, VkFormat.R16G16B16A16SFloat, name: "Ningsha.Bloom.Mip");
                    pyramid[slot].Resize(width, height);
                }
            }
            EnsureInitialized(ctx, VkFormat.R16G16B16A16SFloat);
            BeginFrame(frame);
            for (int i = 0; i < Levels; i++)
            {
                var source = i == 0 ? emission : pyramid[(i - 1) * 2];
                Draw(ctx, source, source, pyramid[i * 2], new Vector4(0.5f / source.Width, 0.5f / source.Height, 0, 0));
                Draw(ctx, pyramid[i * 2], pyramid[i * 2], pyramid[i * 2 + 1], new Vector4(1f / pyramid[i * 2].Width, 0, 1, 0));
                Draw(ctx, pyramid[i * 2 + 1], pyramid[i * 2 + 1], pyramid[i * 2], new Vector4(0, 1f / pyramid[i * 2].Height, 1, 0));
            }
            for (int i = Levels - 2; i >= 0; i--)
            {
                Draw(ctx, pyramid[i * 2], pyramid[(i + 1) * 2], pyramid[i * 2 + 1], new Vector4(0, 0, 2, 0.6f));
                (pyramid[i * 2], pyramid[i * 2 + 1]) = (pyramid[i * 2 + 1], pyramid[i * 2]);
            }
            return pyramid[0];
        }

        //不在每次模糊时重置描述符池，否则前面已录制的命令会失效。
        private void Draw(in FrameContext ctx, RenderTexture source, RenderTexture wide, RenderTexture output, Vector4 parameters)
        {
            SetInput(0, source); SetInput(1, wide); SetPushConstants(parameters);
            RecordPass(ctx.CommandBuffer, ctx.SubmissionFrameIndex, output);
        }

        //窗口的在途帧数量变化时同步重建描述符池和金字塔。
        internal void Rebuild() { ReleaseTargets(); Invalidate(); }

        //所属特性保证 GPU 已停止使用这些纹理。
        private void ReleaseTargets()
        {
            if (textures != null)
                foreach (var pyramid in textures)
                    if (pyramid != null) foreach (var texture in pyramid) texture?.Dispose();
            textures = null;
        }

        //由所属特性在 GPU 停止使用后释放资源。
        public override void Dispose() { ReleaseTargets(); base.Dispose(); }
    }
}
#endif
