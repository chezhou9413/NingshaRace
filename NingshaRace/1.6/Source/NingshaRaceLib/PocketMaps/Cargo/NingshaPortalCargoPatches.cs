using HarmonyLib;
using RimWorld;
using Verse;

using NingshaRaceLib.PocketMaps.Buildings;

namespace NingshaRaceLib.PocketMaps.Cargo
{
    //同步装载流程切换，并让搬运工作等待地下地图生成完成。
    [HarmonyPatch]
    internal static class NingshaPortalCargoPatches
    {
        //整队窗口已经建立新清单，创建领主前切换到允许人员进入的模式。
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnterPortalUtility), nameof(EnterPortalUtility.MakeLordsAsAppropriate))]
        private static void ClearCargoBeforeEnterAccepted(MapPortal portal)
        {
            portal.GetComp<Comp_NingshaPortalCargo>()?.ClearCargoMode();
        }

        //最后一件货物完成装载时立即解除货运锁定。
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapPortal), nameof(MapPortal.Notify_ThingAdded))]
        private static void ClearCargoAfterLoad(MapPortal __instance)
        {
            if (!__instance.LoadInProgress) __instance.GetComp<Comp_NingshaPortalCargo>()?.ClearCargoMode();
        }

        //取消入口同时结束货运模式，避免后续清单沿用旧标记。
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapPortal), nameof(MapPortal.CancelLoad))]
        private static void ClearCargoAfterCancel(MapPortal __instance)
        {
            __instance.GetComp<Comp_NingshaPortalCargo>()?.ClearCargoMode();
        }

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
