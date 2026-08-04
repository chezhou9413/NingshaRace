using AlienRace;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Rendering
{
    //类职责：在侵蚀体状态下阻止 HAR 绘制独立的脸部表情层。
    [HarmonyPatch(typeof(AlienPawnRenderNodeWorker_BodyAddon), nameof(AlienPawnRenderNodeWorker_BodyAddon.CanDrawNow))]
    public static class Patch_ErosionBodyFaceExpression
    {
        //函数职责：保留其他 BodyAddon 的原始可见性，仅关闭侵蚀体脸部表情节点。
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            if (__result && ErosionBodyRenderingUtility.IsErosionFaceExpressionNode(node, parms.pawn))
            {
                __result = false;
            }
        }
    }

    //类职责：在渲染树主线程初始化头部图形时预热侵蚀黑雾材质。
    [HarmonyPatch(typeof(PawnRenderNode), "EnsureMaterialVariantsInitialized", new[] { typeof(Graphic) })]
    public static class Patch_ErosionBodyHeadMaterialPrewarm
    {
        //函数职责：只为侵蚀体的凝砂族头部图形创建四向黑雾材质缓存。
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode __instance, Graphic g)
        {
            Pawn pawn = __instance?.tree?.pawn;
            if (UnityData.IsInMainThread
                && ErosionBodyRenderingUtility.IsErosionHeadNode(__instance, pawn))
            {
                ErosionBodyHeadMaterialPool.PrewarmGraphic(g, pawn);
            }
        }
    }

    //类职责：在侵蚀体头部最终材质提交绘制前替换为 CL 上层黑雾材质。
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.GetFinalizedMaterial))]
    public static class Patch_ErosionBodyHeadFinalMaterial
    {
        //函数职责：主线程允许创建缓存，后台预绘制线程只读取已预热材质。
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Material __result)
        {
            if (ReferenceEquals(__result, null)
                || !ErosionBodyRenderingUtility.IsErosionHeadNode(node, parms.pawn))
            {
                return;
            }

            __result = UnityData.IsInMainThread
                ? ErosionBodyHeadMaterialPool.GetOrCreateMaterial(__result)
                : ErosionBodyHeadMaterialPool.GetMaterial(__result);
        }
    }
}
