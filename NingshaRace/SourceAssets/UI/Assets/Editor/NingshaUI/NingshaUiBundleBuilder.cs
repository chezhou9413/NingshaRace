using System;
using System.IO;
using UnityEditor;
using UnityEngine;

//类职责：仅构建凝砂 IMGUI 使用的独立 Shader 包，不重新打包人物和战斗特效资源。
public static class NingshaUiBundleBuilder
{
    private const string Output = @"E:\RimModDev\NingshaRace\NingshaRace\1.6\AssetBundles";
    private static readonly string[] ShaderAssets =
    {
        "Assets/NingshaUI/Shaders/WeatheredSandstone.shader",
        "Assets/NingshaUI/Shaders/DriftingSand.shader"
    };

    //函数职责：提供菜单和批处理共用的双平台 UI 资源编译入口。
    [MenuItem("RimWorldTools/凝砂界面/构建砂岩界面资源")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateShaders();
        Directory.CreateDirectory(Output);
        BuildPlatform("ningsha_ui.ab", BuildTarget.StandaloneWindows64);
        BuildPlatform("ningsha_ui_mac.ab", BuildTarget.StandaloneOSX);
        Debug.Log("凝砂界面资源构建完成：" + Output);
    }

    //职责：逐项确认底材和流沙着色器可编译，错误信息保留对应资源路径。
    private static void ValidateShaders()
    {
        foreach (string path in ShaderAssets)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("凝砂界面 Shader 不存在或编译错误：" + path);
        }
    }

    //函数职责：用明确资源清单输出指定平台的 Shader 包，并拒绝失败的构建结果。
    private static void BuildPlatform(string name, BuildTarget target)
    {
        AssetBundleBuild[] builds =
        {
            new AssetBundleBuild { assetBundleName = name, assetNames = ShaderAssets }
        };
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(Output, builds,
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, target);
        if (manifest == null) throw new InvalidOperationException("凝砂界面资源构建失败：" + target);
        ValidateShaders();
    }
}
