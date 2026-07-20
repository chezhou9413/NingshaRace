using HarmonyLib;
using NingshaRaceLib.SandGolem;
using Verse;

namespace NingshaRaceLib.Patches
{
    //类职责：显式隐藏沙傀头顶状态图标，避免精神崩溃、惊恐等图标绘制。
    [HarmonyPatch(typeof(PawnRenderNodeWorker_OverlayStatus), nameof(PawnRenderNodeWorker_OverlayStatus.CanDrawNow))]
    public static class Patch_PawnRenderNodeWorker_OverlayStatus_CanDrawNow_SandGolem
    {
        //函数职责：沙傀不绘制头顶状态图标。
        public static void Postfix(PawnDrawParms parms, ref bool __result)
        {
            if (__result && SandGolemUtility.IsSandGolem(parms.pawn))
            {
                __result = false;
            }
        }
    }
}
