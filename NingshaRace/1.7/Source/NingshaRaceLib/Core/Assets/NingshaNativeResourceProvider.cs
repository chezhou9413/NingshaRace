using System;
using ChezhouLib.LibDef;
using ChezhouLib.ALLmap;
using RimUnity.Assets;
using System.IO;
using System.Text.Json;

namespace NingshaRaceLib.Core.Assets;

//在 CL 建立材质模板之前，挂接 RimUnity 标准资源清单。
public sealed class NingshaNativeResourceProvider : ClNativeResourceProvider
{
    private static bool initialized;
    private static Exception failure;

    //资源失败保留具体原因，不回退到旧包或重复初始化半份资源。
    public override void InitializeResources()
    {
        if (initialized) return;
        if (failure != null) throw new InvalidOperationException("凝砂明文资源初始化失败。", failure);
        try
        {
            string package = modContentPack.PackageId.ToLowerInvariant();
            using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(modContentPack.RootDir, "1.7/Data/RimUnity/catalog.json")));
            foreach (var entry in catalog.RootElement.GetProperty("assets").EnumerateArray())
            {
                string path = entry.GetProperty("path").GetString();
                if (path.StartsWith("Shaders/", StringComparison.Ordinal))
                {
                    var exported = AssetLoader.Load<RimUnity.Shader>(modContentPack, path);
                    var shader = HostAssets.ToHost(exported);
                    foreach (string alias in new[] { exported.name, Path.GetFileName(exported.name) })
                    {
                        abDatabase.shaderDataBase[package + "=>" + alias] = shader;
                        abDatabase.shaderDataBase[package + "=>" + alias.ToLowerInvariant()] = shader;
                    }
                }
                else if (path.StartsWith("Textures/", StringComparison.Ordinal))
                {
                    var texture = AssetLoader.Load<RimUnity.Texture2D>(modContentPack, path);
                    abDatabase.Texture2dDataBase.Add(package + "=>" + Path.GetFileName(path), HostAssets.ToHost(texture));
                }
            }
            initialized = true;
        }
        catch (Exception error) { failure = error; throw; }
    }
}
