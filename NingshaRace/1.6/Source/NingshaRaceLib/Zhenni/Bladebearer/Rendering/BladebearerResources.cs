using System;
using System.Collections.Generic;
using System.Linq;
using ChezhouLib.ALLmap;
using ChezhouLib.Startup;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //只在主线程创建共用材质；即时预览与地图提交使用不同材质。
    [StaticConstructorOnStartup]
    internal static class BladebearerResources
    {
        internal const string PackageId = "chezhou.race.ningsharace";
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private static readonly Material colorMaterial, unlitMaterial, depthMaterial, previewColor, previewDepth;
        private static readonly Material particleMaterial, previewParticle;
        internal static readonly Material BloomSource, PreviewBloomSource;
        internal static readonly Mesh Mesh;
        internal static Material PhaseColor => unlitMaterial;

        //预加载默认资源，渲染树并行准备阶段只引用已创建对象。
        static BladebearerResources()
        {
            InitiaUnityShaderLord.EnsureInitialized();
            colorMaterial = Create("NingshaRace/Zhenni/Bladebearer/Flame");
            //光照遮罩是一层后绘制的网格；只调整火焰队列，仍使用原矩阵和 LEqual 深度测试。
            unlitMaterial = new Material(colorMaterial)
            {
                renderQueue = Mathf.Max(colorMaterial.renderQueue,
                    Mathf.Max(MatBases.LightOverlay.renderQueue, MatBases.LightOverlayGravship.renderQueue) + 1)
            };
            depthMaterial = Create("NingshaRace/Zhenni/Bladebearer/Depth");
            particleMaterial = Create("NingshaRace/Zhenni/Bladebearer/Particles");
            particleMaterial.renderQueue = unlitMaterial.renderQueue;
            previewParticle = new Material(particleMaterial);
            BloomSource = Create("NingshaRace/Zhenni/Bladebearer/BloomSource");
            PreviewBloomSource = new Material(BloomSource);
            previewColor = new Material(colorMaterial);
            previewDepth = new Material(depthMaterial);
            Mesh = MeshMakerPlanes.NewPlaneMesh(1f, false, false);
            Mesh.bounds = new Bounds(Vector3.zero, new Vector3(1.8f, 0.02f, 1.8f));
            foreach (BladebearerAppearanceDef def in DefDatabase<BladebearerAppearanceDef>.AllDefs)
                foreach (BladebearerPart part in def.parts) ValidateTextures(part);
        }

        //明确报告未打包或不支持的 Shader。
        internal static Material Create(string name)
        {
            Shader shader = abDatabase.GetShader(name, PackageId);
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("震尼拥刀者 Shader 无法加载：" + name + "，请检查 ningsha_zhenni_bladebearer.ab。");
            return new Material(shader) { name = name, hideFlags = HideFlags.HideAndDontSave };
        }

        //选择即时绘制或地图渲染材质，防止预览覆盖已提交的地图材质。
        internal static Material Material(BladebearerPart part, bool depth, bool immediate)
        {
            if (depth) return immediate ? previewDepth : depthMaterial;
            if (part.particle) return immediate ? previewParticle : particleMaterial;
            if (immediate) return previewColor;
            return part.flame || part.additive > 0 ? unlitMaterial : colorMaterial;
        }

        //贴图只首次从 Mod 内容加载，缺失时包含部件路径。
        internal static Texture2D Texture(string path)
        {
            if (textures.TryGetValue(path, out Texture2D texture)) return texture;
            texture = ContentFinder<Texture2D>.Get(path, false);
            if (texture == null) throw new InvalidOperationException("震尼拥刀者贴图不存在：" + path);
            textures.Add(path, texture);
            return texture;
        }

        //置换图保留线性、Repeat 和 CPU 读取，供火焰与粒子共用波动。
        internal static Texture2D Noise(string name)
        {
            if (!abDatabase.Texture2dDataBase.TryGetValue(PackageId + "=>" + name, out Texture2D texture))
                throw new InvalidOperationException("置换图未注册：" + name + "，需要加入震尼拥刀者 AssetBundle。");
            if (!texture.isReadable)
                throw new InvalidOperationException("置换图「" + name + "」不可读取；请开启 Read/Write 并重新构建震尼拥刀者 AssetBundle。");
            return texture;
        }

        //导入失败时向编辑窗口报告具体部件，不应用一半参数。
        internal static void ValidateTextures(BladebearerPart part)
        {
            try { Texture(part.texture); Noise(part.displacementTexture); }
            catch (InvalidOperationException e) { throw new InvalidOperationException("部件「" + part.label + "」：" + e.Message, e); }
        }

        //提供本 Mod 已打包置换图的选择列表。
        internal static IEnumerable<string> NoiseNames() => abDatabase.Texture2dDataBase.Keys
            .Where(k => k.StartsWith(PackageId + "=>", StringComparison.Ordinal))
            .Select(k => k.Substring(PackageId.Length + 2)).OrderBy(k => k);
    }
}
