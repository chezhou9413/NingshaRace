using HarmonyLib;
using RimWorld;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Patches
{
    //类职责：只放大菌巢附近植物的有效生长速度，保留原版温度、光照、休眠与病害限制。
    [HarmonyPatch(typeof(Plant), nameof(Plant.GrowthRate), MethodType.Getter)]
    public static class Patch_Plant_FungalGrowth
    {
        //函数职责：对已生成且仍能生长的植物应用本格最高倍率，不加速衰老和其他长周期行为。
        public static void Postfix(Plant __instance, ref float __result)
        {
            if (__result > 0f && __instance.Spawned)
                __result *= __instance.Map.GetComponent<MapComponent_AntHabitats>().GrowthMultiplierAt(__instance.Position);
        }
    }
}
