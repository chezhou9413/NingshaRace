using AlienRace;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering.BodyAddons
{
    //类职责：把按贴图配置的倍率叠加到 HAR BodyAddon 已计算完成的绘制缩放上。
    [HarmonyPatch(typeof(AlienPawnRenderNodeWorker_BodyAddon), nameof(AlienPawnRenderNodeWorker_BodyAddon.ScaleFor))]
    public static class Patch_BodyAddonScaleFor_TextureRule
    {
        //函数职责：在 HAR 保留原有体型缩放后，对命中的单张方向纹理应用额外倍率。
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (!BodyAddonTextureScaleUtility.TryGetTransform(node, parms, out Vector2 scale, out _, out _))
            {
                return;
            }

            __result.x *= scale.x;
            __result.z *= scale.y;
        }
    }
}
