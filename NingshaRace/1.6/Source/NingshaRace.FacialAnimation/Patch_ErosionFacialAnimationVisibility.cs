using FacialAnimation;
using HarmonyLib;
using NingshaRaceLib.Erosion.Utility;
using Verse;

namespace NingshaRaceLib.Compatibility.FA
{
    //侵蚀体隐藏独立面部表情，保留原版或 HAR 的头发及装饰节点。
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.CanDrawNow))]
    internal static class Patch_ErosionFacialAnimationVisibility
    {
        //FA 的眼、嘴、眉以及头部遮盖层都不能覆盖黑脸；凝砂不叠加第二个 FA 头部。
        [HarmonyPostfix]
        private static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            if (__result && node is NLFacialAnimationPartNode && ErosionPawnUtility.IsErosionBody(parms.pawn))
                __result = !ErosionPawnUtility.IsNingshaErosionBody(parms.pawn)
                    && ErosionFacialAnimationUtility.IsBaseHead(node);
        }
    }
}
