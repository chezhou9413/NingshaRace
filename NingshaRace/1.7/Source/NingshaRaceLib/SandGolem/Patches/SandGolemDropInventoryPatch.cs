using HarmonyLib;
using NingshaRaceLib.SandGolem.Utility;
using Verse;

namespace NingshaRaceLib.SandGolem.Patches
{
    //沙傀自然消散、被收回或被销毁前先掉落背包和持有物。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Destroy))]
    internal static class SandGolemDropInventoryPatch
    {
        //在实体离图之前使用原版容器掉落流程，保留物品内容和堆叠。
        private static void Prefix(Pawn __instance)
        {
            if (!SandGolemUtility.IsSandGolem(__instance) || __instance.Destroyed
                || !__instance.SpawnedOrAnyParentSpawned) return;
            __instance.inventory?.DropAllNearPawn(__instance.PositionHeld);
            __instance.equipment?.DropAllEquipment(__instance.PositionHeld);
            __instance.carryTracker?.TryDropCarriedThing(__instance.PositionHeld, ThingPlaceMode.Near, out _);
        }
    }
}
