using HarmonyLib;
using NingshaRaceLib.Erosion.Utility;
using Verse;

namespace NingshaRaceLib.Erosion.Patches
{
    //侵蚀体不受环境低温造成的失温和冻伤影响。
    [HarmonyPatch(typeof(HediffGiver_Hypothermia), nameof(HediffGiver_Hypothermia.OnIntervalPassed))]
    internal static class ErosionHypothermiaPatch
    {
        //停止侵蚀体的低温病变累积，其他生物继续走原版逻辑。
        private static bool Prefix(Pawn pawn) => !ErosionPawnUtility.IsErosionBody(pawn);
    }
}
