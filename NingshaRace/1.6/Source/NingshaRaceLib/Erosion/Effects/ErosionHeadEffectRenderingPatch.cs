using HarmonyLib;
using UnityEngine;
using Verse;

using NingshaRaceLib.Erosion.Rendering;

namespace NingshaRaceLib.Erosion.Effects
{
    //类职责：在凝砂头部节点完成地图绘制后取得最终矩阵，并把准确的头部锚点交给侵蚀烟雾管理器。
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.PostDraw))]
    public static class Patch_ErosionHeadEffectRendering
    {
        //函数职责：忽略肖像与其他节点，只在当前地图的侵蚀体头部完成绘制后更新烟雾位置。
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, Matrix4x4 matrix)
        {
            if (parms.Portrait || !ErosionBodyRenderingUtility.IsErosionHeadNode(node, parms.pawn))
            {
                return;
            }

            ErosionHeadEffectManager.UpdateForDraw(parms.pawn, matrix);
        }
    }
}
