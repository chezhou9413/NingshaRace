#if RW_1_7
using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorldWaylith.Rendering;
using Verse;
using Waylith;
using Waylith.Rendering.PostFx;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //将刀兵辉光接到地图合成之后、界面绘制之前。
    internal static class BladebearerBloomCamera
    {
        private static bool attached;
        internal static string ShaderDirectory => Path.Combine(
            LoadedModManager.RunningModsListForReading.First(m =>
                string.Equals(m.PackageIdPlayerFacing, BladebearerResources.PackageId, StringComparison.OrdinalIgnoreCase)).RootDir,
            "1.7", "Data", "Rendering", "Bladebearer");

        //只注册一次；特性是否绘制由每帧主线程快照决定。
        internal static void EnsureAttached()
        {
            if (attached) return;
            EngineApplication app = EngineApplication.Instance;
            if (app == null || app.Headless) return;
            var renderer = app.FrameRenderer;
            MapRenderFeature map = null;
            int compositeIndex = -1;
            for (int i = 0; i < renderer.GetFeatureCount(); i++)
            {
                var feature = renderer.GetFeatureAt(i);
                if (feature is MapRenderFeature mapFeature) map = mapFeature;
                if (feature is PostFxFeature) compositeIndex = i;
            }
            if (map == null || compositeIndex < 0)
                throw new InvalidOperationException("刀兵辉光无法找到 Waylith 地图及后处理特性。");
            var targets = (SceneColorTargets)AccessTools.Field(typeof(MapRenderFeature), "sceneTargets").GetValue(map);
            renderer.InsertFeature(compositeIndex + 1, new BladebearerBloomFeature(app, map, targets, ShaderDirectory));
            attached = true;
        }
    }
}
#endif
