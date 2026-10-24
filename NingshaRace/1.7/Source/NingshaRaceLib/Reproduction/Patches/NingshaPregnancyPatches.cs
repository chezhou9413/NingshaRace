using HarmonyLib;
using RimWorld;
using Verse;

using NingshaRaceLib.Reproduction.Utility;

namespace NingshaRaceLib.Reproduction.Patches
{
    //类职责：保留凝砂完整原版怀孕流程，并将最终生产结果替换为受精卵。
    [HarmonyPatch]
    public static class NingshaPregnancyPatches
    {
        //类职责：在原版生产结算点将凝砂子代替换为受精凝砂卵。
        [HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome))]
        private static class Patch_ApplyBirthOutcome
        {
            //函数职责：跳过原版婴儿生成并复用正式凝砂产卵流程。
            private static bool Prefix(Thing birtherThing, Pawn father, bool preventLetter, ref Thing __result)
            {
                if (!(birtherThing is Pawn mother) || !NingshaReproductionUtility.IsNingsha(mother))
                {
                    return true;
                }

                __result = NingshaReproductionUtility.CompleteOriginalBirthAsEgg(mother, father, preventLetter);
                return false;
            }
        }
    }
}
