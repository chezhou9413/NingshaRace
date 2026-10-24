using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using RimWorld;
using UnityEngine;

namespace NingshaRaceLib.DesertPit.Ecology.Patches
{
    //类职责：让菌巢温床中的食用菌获得可调的实际收成，玩家与蚂蚁使用相同产量。
    [HarmonyPatch(typeof(Plant), nameof(Plant.YieldNow))]
    public static class Patch_Plant_FungalHarvest
    {
        //函数职责：只放大产出生菌的植物收成，不增加药材、木材或其他作物产量。
        public static void Postfix(Plant __instance, ref int __result)
        {
            if (__result > 0 && __instance.Spawned && __instance.def.plant.harvestedThingDef == DefOfRefs.RawFungus)
                __result = Mathf.RoundToInt(__result * __instance.Map.GetComponent<MapComponent_AntHabitats>().HarvestMultiplierAt(__instance.Position));
        }
    }
}
