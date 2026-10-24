using HarmonyLib;
using NingshaRaceLib.Erosion.Rendering;
using NingshaRaceLib.Erosion.Utility;
using Verse;

namespace NingshaRaceLib.Compatibility.FA
{
    //将 FA 基础头部接入主程序集的材质、烟雾锚点与绘制准备流程。
    [HarmonyPatch(typeof(ErosionBodyRenderingUtility), nameof(ErosionBodyRenderingUtility.IsErosionHeadNode))]
    internal static class Patch_ErosionFacialAnimationHead
    {
        //凝砂继续使用自身完整头部附加层，其他种族使用 FA 的真实头部。
        [HarmonyPostfix]
        private static void Postfix(PawnRenderNode node, Pawn pawn, ref bool __result)
        {
            if (!__result && ErosionPawnUtility.IsErosionBody(pawn)
                && !ErosionPawnUtility.IsNingshaErosionBody(pawn))
                __result = ErosionFacialAnimationUtility.IsBaseHead(node);
        }
    }
}
