using HarmonyLib;
using RimWorld;
using Verse;

using NingshaRaceLib.PocketMaps.Buildings;

namespace NingshaRaceLib.PocketMaps.Cargo
{
    //类职责：让原版整队进入和搬运工作共同遵守凝砂族分帧地图生成状态。
    [HarmonyPatch]
    internal static class NingshaPortalCargoPatches
    {
        //函数职责：原版整队窗口建立进入 Lord 后立即启动凝砂族分帧地图生成。
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnterPortalUtility), nameof(EnterPortalUtility.MakeLordsAsAppropriate))]
        private static void BeginGenerationAfterEnterAccepted(MapPortal portal)
        {
            if (portal is Building_NingshaPocketMapPortal gate && portal.LoadInProgress && !gate.PocketMapExists)
            {
                gate.BeginPocketMapGeneration();
            }
        }

        //函数职责：在地下地图生成完成前阻止搬运者领取工作，避免首件货物触发同步生成。
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnterPortalUtility), nameof(EnterPortalUtility.HasJobOnPortal))]
        private static void BlockHaulingDuringGeneration(MapPortal portal, ref bool __result)
        {
            if (portal is Building_NingshaPocketMapPortal gate && !gate.PocketMapExists)
            {
                __result = false;
            }
        }
    }
}
