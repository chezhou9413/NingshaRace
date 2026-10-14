using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RimUnity.Editor;
using UnityEditor;
using UnityEngine;

//通过 RimUnity 标准导出器生成运行资源，保留源预制体和 Shader。
public static class NingshaRimUnityExport
{
    private const string ModRoot = "E:/RimModDev/NingshaRace/NingshaRace";

    //批处理结束时恢复工具配置，任一入口失败都使构建失败。
    public static void Export()
    {
        string oldTool = EditorPrefs.GetString("RimUnity.ShaderTool", "");
        string oldGame = EditorPrefs.GetString("RimUnity.GameRoot", "");
        var results = new List<BundleAssetResult>();
        try
        {
            EditorPrefs.SetString("RimUnity.ShaderTool", "E:/RimModDev/RimUnity/Tools/bin/ShaderTool/RimUnity.ShaderTool.exe");
            EditorPrefs.SetString("RimUnity.GameRoot", "E:/steam/steamapps/common/RimWorld");
            foreach (string path in Directory.GetFiles("Assets", "*.shader", SearchOption.AllDirectories).OrderBy(p => p))
                if (!Path.GetFileName(path).StartsWith("BladebearerBloom", StringComparison.Ordinal))
                    Write(path.Replace('\\', '/'), "Shaders/" + Path.GetFileNameWithoutExtension(path));
            foreach (string name in new[] { "SandThrowBurst", "PetrifyingSandwave", "PetrifyingSandHit", "qinshiyanwu" })
            {
                string path = "Assets/Prefabs/VFX/" + name + ".prefab";
                Write(path, "Bundles/ningsharace/" + path);
            }
            Write("Assets/Combat/MeleeAnimation/Textures/MeleeFlameDisplacement.png", "Textures/MeleeFlameDisplacement");
            Write("Assets/Zhenni/Bladebearer/Textures/BladebearerDisplacement.png", "Textures/BladebearerDisplacement");
            string report = ModRoot + "/Tools/Build/RimUnity-export-report.json";
            File.WriteAllText(report, JsonConvert.SerializeObject(results, Formatting.Indented), new System.Text.UTF8Encoding(false));
            if (results.Any(r => r.status != "exported"))
                throw new InvalidOperationException("凝砂 RimUnity 导出失败，查看 " + report);
            Debug.Log("NINGSHA_RIMUNITY_EXPORT_OK " + results.Count);
        }
        finally
        {
            EditorPrefs.SetString("RimUnity.ShaderTool", oldTool);
            EditorPrefs.SetString("RimUnity.GameRoot", oldGame);
        }

        //资源参数由标准导出器读取，失败记录保留源路径和完整原因。
        void Write(string source, string logical)
        {
            var result = new BundleAssetResult { path = source };
            try
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(source);
                if (asset == null) throw new FileNotFoundException("源资源不存在：" + source);
                PrefabExporter.Export(ModRoot, new ExportRequest { asset = asset, path = logical, description = "凝砂族：" + source });
                result.type = asset.GetType().Name;
                result.status = "exported";
            }
            catch (Exception error)
            {
                result.status = "failed";
                result.error = error.ToString();
                Debug.LogError(source + "\n" + error);
            }
            results.Add(result);
        }
    }
}
