using HarmonyLib;
using RimWorld;
using Verse;
using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Patches
{
    //类职责：让强制唤起侵蚀体尸体的操作保留侵蚀身份与服装，不进入蹒跚怪转化流程。
    [HarmonyPatch(typeof(MutantUtility), nameof(MutantUtility.ResurrectAsShambler))]
    public static class Patch_ErosionBodyShamblerResurrection
    {
        //函数职责：把侵蚀体的蹒跚怪复活请求交给普通复活流程，其余尸体继续执行原版逻辑。
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn)
        {
            if (!ErosionPawnUtility.HasErosionBodyIdentity(pawn))
            {
                return true;
            }

            ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams { noLord = true });
            return false;
        }
    }
}
