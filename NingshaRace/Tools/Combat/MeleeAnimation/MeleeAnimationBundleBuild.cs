using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NingshaRace.Combat.MeleeAnimation.Editor
{
    //通用近战效果独立打包，单位外观不成为框架依赖。
    public static class MeleeAnimationBundleBuild
    {
        private const string Root = "Assets/Combat/MeleeAnimation/";
        //只构建 Windows 资源，不进入场景或播放模式。
        [MenuItem("RimWorldTools/近战动画/构建 Windows 资源")]
        public static void BuildWindows()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string noisePath = Root + "Textures/MeleeFlameDisplacement.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(noisePath);
            importer.sRGBTexture = false; importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            string shaderPath = Root + "Shader/MeleeFlameTrail.shader";
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            Check(shader);
            string staging = "Library/MeleeAnimationBundle/Windows"; Directory.CreateDirectory(staging);
            var manifest = BuildPipeline.BuildAssetBundles(staging, new[] { new AssetBundleBuild
            { assetBundleName = "ningsha_melee.ab", assetNames = new[] { shaderPath, noisePath } } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows64);
            if (manifest == null) throw new InvalidOperationException("近战动画 Windows AssetBundle 构建失败。");
            Check(shader);
            const string mod = "E:/RimModDev/NingshaRace/NingshaRace/";
            File.Copy(staging + "/ningsha_melee.ab", mod + "1.6/AssetBundles/ningsha_melee.ab", true);
            string sources = mod + "Tools/Combat/MeleeAnimation"; Directory.CreateDirectory(sources);
            foreach (string path in new[] { shaderPath, noisePath, Root + "Editor/MeleeAnimationBundleBuild.cs" })
                File.Copy(path, Path.Combine(sources, Path.GetFileName(path)), true);
            Debug.Log("MELEE_ANIMATION_BUILD_OK");
        }
        //Shader 编译错误阻止发布资源。
        private static void Check(Shader shader)
        {
            if (shader == null) throw new InvalidOperationException("找不到 MeleeFlameTrail.shader。");
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors.Select(e => e.message)));
        }
    }
}
