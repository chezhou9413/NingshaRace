using HarmonyLib;
using RimWorld;
using Verse;
using NingshaRaceLib.Race.Utility;

namespace NingshaRaceLib.Erosion.Patches
{
    //阻止直接调用转化入口绕过凝砂相关生物的蹒跚怪免疫。
    [HarmonyPatch(typeof(MutantUtility), nameof(MutantUtility.ResurrectAsShambler))]
    public static class Patch_ErosionBodyShamblerResurrection
    {
        //免疫对象保持尸体状态，普通复活入口不受影响。
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn)
        {
            return !NingshaShamblerUtility.IsImmune(pawn);
        }
    }
}
