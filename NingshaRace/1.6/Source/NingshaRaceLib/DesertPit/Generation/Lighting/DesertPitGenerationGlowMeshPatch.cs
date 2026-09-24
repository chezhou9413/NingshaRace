using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Lighting
{
    //类职责：把地下生成期间首次光照计算的绘制通知留给地图初始化时的完整网格重建。
    [HarmonyPatch(typeof(MapDrawer), nameof(MapDrawer.WholeMapChanged))]
    internal static class DesertPitGenerationGlowMeshPatch
    {
        //函数职责：仅在本模块刷新光照且绘制分区尚未创建时跳过光照网格通知，其余通知照常执行。
        private static bool Prefix(MapDrawer __instance, Section[,] ___sections, ulong change)
        {
            //FinalizeInit 会通过 RegenerateEverythingNow 创建分区并读取已计算的光照。
            return ___sections != null || change != (ulong)MapMeshFlagDefOf.GroundGlow
                || !DesertPitGenerationLighting.IsRefreshing(__instance);
        }
    }
}
