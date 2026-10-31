using HarmonyLib;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Race.Rendering.BodyAddons;

namespace NingshaRaceLib.Core.Bootstrap
{
    //模组启动入口类，负责在 RimWorld 加载时注册当前程序集内的 Harmony 补丁。
    [StaticConstructorOnStartup]
    public static class MainEntry
    {
        //启动时只挂载常驻补丁，异步时序分类由首次启用入口负责挂载。
        static MainEntry()
        {
            var harmony = new Harmony("chezhou.race.ningsharace");
            harmony.PatchAllUncategorized(typeof(MainEntry).Assembly);
            BodyAddonLinkFallbackUtility.Initialize();
            BodyAddonTextureScaleUtility.Initialize();
        }
    }
}
