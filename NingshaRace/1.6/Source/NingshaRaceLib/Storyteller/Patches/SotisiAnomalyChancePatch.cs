using HarmonyLib;
using RimWorld;
using UnityEngine;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Storyteller.Patches
{
    //类职责：仅在索提斯担任叙事者时提高异常事件池被选中的概率。
    [HarmonyPatch(typeof(RimWorld.Storyteller), nameof(RimWorld.Storyteller.AnomalyIncidentChanceNow), MethodType.Getter)]
    public static class SotisiAnomalyChancePatch
    {
        private const float AnomalyChanceFactor = 2f;

        //函数职责：将索提斯的当前异常事件概率提高到原版两倍并限制在有效概率范围内。
        public static void Postfix(RimWorld.Storyteller __instance, ref float __result)
        {
            if (__instance?.def == DefOfRefs.Ningsha_SotisiStoryteller)
            {
                __result = Mathf.Clamp01(__result * AnomalyChanceFactor);
            }
        }
    }
}
