using HarmonyLib;
using NingshaRaceLib.Race.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Race.Patches
{
    //死亡之雾等事件筛选尸体时直接排除免疫对象。
    [HarmonyPatch(typeof(MutantUtility), nameof(MutantUtility.CanResurrectAsShambler))]
    internal static class NingshaShamblerEligibilityPatch
    {
        //保留普通尸体的原版判定。
        private static void Postfix(Corpse corpse, ref bool __result)
        {
            if (__result && NingshaShamblerUtility.IsImmune(corpse.InnerPawn)) __result = false;
        }
    }
}
