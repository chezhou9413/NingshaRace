using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NingshaRace.SandGolem.Editor
{
    //编译沙傀聚散 Shader 并输出独立资源包。
    public static class SandGolemBundleBuild
    {
        private const string ShaderPath = "Assets/SandGolem/Shader/PawnWindSandify.shader";
        private const string OutputRoot = "E:/ModdevMics/Outputs/NingshaRace/SandGolem";
        private const string Destination = "E:/RimModDev/NingshaRace/NingshaRace/1.6/AssetBundles";

        //为模组已有的两个桌面平台生成资源。
        [MenuItem("RimWorldTools/沙傀/构建聚散资源")]
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CheckShader();
            BuildPlatform(BuildTarget.StandaloneWindows64, "Windows", "ningsha_sandgolem.ab");
            BuildPlatform(BuildTarget.StandaloneOSX, "Mac", "ningsha_sandgolem_mac.ab");
            Directory.CreateDirectory(Destination);
            Publish("Windows", "ningsha_sandgolem.ab");
            Publish("Mac", "ningsha_sandgolem_mac.ab");
            Debug.Log("SANDGOLEM_BUILD_OK " + Destination);
        }

        //只构建当前 Shader，任何编译错误都终止输出。
        private static void BuildPlatform(BuildTarget target, string folder, string bundleName)
        {
            string output = Path.Combine(OutputRoot, folder);
            Directory.CreateDirectory(output);
            var bundle = new AssetBundleBuild { assetBundleName = bundleName, assetNames = new[] { ShaderPath } };
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(output, new[] { bundle },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, target);
            if (manifest == null) throw new InvalidOperationException("沙傀资源包构建失败：" + target);
            CheckShader();
        }

        //两端均编译成功后再复制模组实际加载的资源。
        private static void Publish(string folder, string bundleName)
        {
            string source = Path.Combine(OutputRoot, folder, bundleName);
            File.Copy(source, Path.Combine(Destination, bundleName), true);
            File.Copy(source + ".manifest", Path.Combine(Destination, bundleName + ".manifest"), true);
        }

        //保留具体 Shader 编译诊断，避免用错误材质替换成品。
        private static void CheckShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) throw new InvalidOperationException("找不到沙傀 Shader：" + ShaderPath);
            var errors = ShaderUtil.GetShaderMessages(shader)
                .Where(message => message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            if (errors.Length > 0)
                throw new InvalidOperationException(string.Join("\n", errors.Select(error => error.message)));
            if (ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("沙傀 Shader 存在编译错误。");
        }
    }
}
