using HarmonyLib;
using NingshaRaceLib.Rendering.BodyAddons;
using Verse;

namespace NingshaRaceLib
{
    //模组启动入口类，负责在 RimWorld 加载时注册当前程序集内的 Harmony 补丁。
    [StaticConstructorOnStartup]
    public static class MainEntry
    {
        //静态构造函数负责创建 Harmony 实例并扫描当前程序集中的补丁。
        static MainEntry()
        {
            var harmony = new Harmony("chezhou.race.ningsharace");
            harmony.PatchAll(typeof(MainEntry).Assembly);
            BodyAddonLinkFallbackUtility.Initialize();
            BodyAddonTextureScaleUtility.Initialize();
        }
    }
}
