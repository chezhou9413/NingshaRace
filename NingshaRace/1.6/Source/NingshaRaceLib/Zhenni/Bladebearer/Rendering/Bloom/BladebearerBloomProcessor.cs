using System;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //沿用 Rave 的选择性辉光流程：多尺度模糊后以柔和方式合成，不提取整个场景。
    internal sealed class BladebearerBloomProcessor : IDisposable
    {
        private const int MaxLevels = 4;
        private readonly Material blur = BladebearerResources.Create("NingshaRace/Zhenni/Bladebearer/BloomBlur");
        private readonly Material composite = BladebearerResources.Create("NingshaRace/Zhenni/Bladebearer/BloomComposite");
        private readonly RenderTexture[] levels = new RenderTexture[MaxLevels];
        private readonly RenderTexture[] scratch = new RenderTexture[MaxLevels];

        //输入已经按各部件阈值提取亮部，并完成地图深度遮挡。
        internal void Render(RenderTexture source, RenderTexture emission, RenderTexture destination)
        {
            try
            {
                int width = Mathf.Max(1, source.width / 2), height = Mathf.Max(1, source.height / 2);
                int count = 0;
                for (int i = 0; i < MaxLevels; i++)
                {
                    levels[i] = Allocate(width, height); scratch[i] = Allocate(width, height); count++;
                    if (i == 0) Graphics.Blit(emission, levels[i], blur, 3);
                    else Graphics.Blit(levels[i - 1], levels[i], blur, 1);
                    blur.SetVector("_Direction", new Vector4(1, 0, 0, 0));
                    Graphics.Blit(levels[i], scratch[i], blur, 0);
                    blur.SetVector("_Direction", new Vector4(0, 1, 0, 0));
                    Graphics.Blit(scratch[i], levels[i], blur, 0);
                    if (width <= 2 || height <= 2) break;
                    width = Mathf.Max(1, width / 2); height = Mathf.Max(1, height / 2);
                }
                blur.SetFloat("_Scatter", 0.6f);
                for (int i = count - 2; i >= 0; i--)
                {
                    blur.SetTexture("_LowMip", levels[i + 1]);
                    Graphics.Blit(levels[i], scratch[i], blur, 2);
                    Graphics.Blit(scratch[i], levels[i]);
                }
                composite.SetTexture("_BloomTex", levels[0]);
                Graphics.Blit(source, destination, composite, 0);
            }
            finally
            {
                composite.SetTexture("_BloomTex", null); blur.SetTexture("_LowMip", null);
                ReleaseLevels();
            }
        }

        //半精度纹理来自 Unity 临时池，不逐帧创建持久资源。
        private static RenderTexture Allocate(int width, int height)
        {
            RenderTexture result = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);
            result.filterMode = FilterMode.Bilinear; result.wrapMode = TextureWrapMode.Clamp;
            return result;
        }

        //无论渲染是否完成都归还已经取得的纹理。
        private void ReleaseLevels()
        {
            for (int i = 0; i < MaxLevels; i++)
            {
                if (levels[i] != null) RenderTexture.ReleaseTemporary(levels[i]);
                if (scratch[i] != null) RenderTexture.ReleaseTemporary(scratch[i]);
                levels[i] = null; scratch[i] = null;
            }
        }

        //由相机或预览窗口的生命周期释放材质。
        public void Dispose()
        {
            ReleaseLevels();
            UnityEngine.Object.Destroy(blur); UnityEngine.Object.Destroy(composite);
        }
    }
}
