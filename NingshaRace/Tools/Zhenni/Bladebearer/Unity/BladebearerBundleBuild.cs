using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NingshaRace.Zhenni.Bladebearer.Editor
{
    //只构建刀兵 Shader 与置换图，不触碰项目其他 AssetBundle。
    public static class BladebearerBundleBuild
    {
        private const string ModRoot = "E:/RimModDev/NingshaRace/NingshaRace";
        private const string Noise = "Assets/Zhenni/Bladebearer/Textures/BladebearerDisplacement.png";
        private static readonly string[] Shaders =
        {
            "Assets/Zhenni/Bladebearer/Shader/BladebearerFlame.shader", "Assets/Zhenni/Bladebearer/Shader/BladebearerDepth.shader", "Assets/Zhenni/Bladebearer/Shader/BladebearerParticles.shader",
            "Assets/Zhenni/Bladebearer/Shader/BladebearerBloomSource.shader", "Assets/Zhenni/Bladebearer/Shader/BladebearerBloomBlur.shader",
            "Assets/Zhenni/Bladebearer/Shader/BladebearerBloomComposite.shader",
            "Assets/Zhenni/Bladebearer/Shader/BladebearerPhaseEmbers.shader"
        };

        //供 Unity 菜单和批处理命令共同调用。
        [MenuItem("RimWorldTools/刀兵/构建 Windows Mod 资源")]
        public static void BuildWindows()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var noise = (TextureImporter)AssetImporter.GetAtPath(Noise);
            noise.textureType = TextureImporterType.Default;
            noise.sRGBTexture = false; noise.alphaIsTransparency = false;
            noise.wrapMode = TextureWrapMode.Repeat; noise.filterMode = FilterMode.Bilinear;
            noise.mipmapEnabled = false; noise.textureCompression = TextureImporterCompression.Uncompressed;
            //火星在 CPU 读取同一张 RG 置换图，与火焰保持相同的波动节奏。
            noise.isReadable = true;
            noise.SaveAndReimport();
            CheckShaders();
            string temporary = "Library/BladebearerBundle/Windows";
            Directory.CreateDirectory(temporary);
            var build = new AssetBundleBuild
            {
                assetBundleName = "ningsha_zhenni_bladebearer.ab", assetNames = Shaders.Concat(new[] { Noise }).ToArray()
            };
            var manifest = BuildPipeline.BuildAssetBundles(temporary, new[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows64);
            if (manifest == null) throw new InvalidOperationException("刀兵 Windows AssetBundle 构建失败。");
            CheckShaders();
            string destination = ModRoot + "/1.6/AssetBundles";
            Directory.CreateDirectory(destination);
            File.Copy(temporary + "/ningsha_zhenni_bladebearer.ab", destination + "/ningsha_zhenni_bladebearer.ab", true);
            string source = ModRoot + "/Tools/Zhenni/Bladebearer/Unity";
            Directory.CreateDirectory(source);
            foreach (string path in Shaders.Concat(new[] { "Assets/Zhenni/Bladebearer/Shader/BladebearerFlameCommon.cginc", Noise,
                "Assets/Zhenni/Bladebearer/Editor/BladebearerBundleBuild.cs" }))
                File.Copy(path, Path.Combine(source, Path.GetFileName(path)), true);
            Debug.Log("BLADEBEARER_BUILD_OK " + destination + "/ningsha_zhenni_bladebearer.ab");
        }

        //所有 Shader 错误必须阻止部署资源包。
        private static void CheckShaders()
        {
            foreach (string path in Shaders)
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) throw new InvalidOperationException("找不到 Shader：" + path);
                if (ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Shader 存在编译错误：" + path);
                var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
                if (errors.Length > 0) throw new InvalidOperationException(path + "\n" + string.Join("\n", errors.Select(e => e.message)));
            }
        }
    }
}
