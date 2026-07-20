using AlienRace;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering.BodyAddons
{
    //类职责：把按贴图配置的位置和层级偏移叠加到 HAR BodyAddon 已计算完成的绘制位置上。
    [HarmonyPatch(typeof(AlienPawnRenderNodeWorker_BodyAddon), nameof(AlienPawnRenderNodeWorker_BodyAddon.OffsetFor))]
    public static class Patch_BodyAddonOffsetFor_TextureRule
    {
        //函数职责：保留 HAR 原始位置，并为命中的单张方向纹理追加位置和层级偏移。
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (!BodyAddonTextureScaleUtility.TryGetTransform(node, parms, out _, out Vector2 offset, out float layerOffset))
            {
                return;
            }

            __result.x += offset.x;
            __result.y += layerOffset;
            __result.z += offset.y;
        }
    }
}
