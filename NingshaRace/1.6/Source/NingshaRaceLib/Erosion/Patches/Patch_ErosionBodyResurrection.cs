using HarmonyLib;
using RimWorld;
using Verse;
using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Patches
{
    //类职责：在普通复活完成后恢复仅剩永久健康标记的侵蚀体身份。
    [HarmonyPatch(typeof(ResurrectionUtility), nameof(ResurrectionUtility.TryResurrect))]
    public static class Patch_ErosionBodyResurrection
    {
        //函数职责：在原版健康恢复和尸体移除前记录该人物是否属于侵蚀体。
        [HarmonyPrefix]
        public static void Prefix(Pawn pawn, out bool __state)
        {
            __state = ErosionPawnUtility.HasErosionBodyIdentity(pawn);
        }

        //函数职责：复活成功且侵蚀身份缺失时安装完整的异变状态、实体阵营和专属能力。
        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, bool __state, bool __result)
        {
            if (__result && __state && !ErosionPawnUtility.IsErosionBody(pawn))
            {
                ErosionBodySpawnUtility.TurnIntoErosionBody(pawn);
            }
        }
    }
}
