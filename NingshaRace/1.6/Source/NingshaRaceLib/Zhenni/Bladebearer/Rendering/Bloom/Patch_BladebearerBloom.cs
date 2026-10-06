using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //在原版更新阶段接入地图相机，后续捕获发生在相机开始渲染时。
    [HarmonyPatch(typeof(Root_Play), nameof(Root_Play.Update))]
    internal static class Patch_BladebearerBloomCamera
    {
        //复用地图相机的原生图像回调，不在界面阶段截屏。
        [HarmonyPostfix]
        private static void Postfix() { BladebearerBloomCamera.EnsureAttached(); }
    }

    //Pawn 相机在 OnPostRender 中即时画角色，必须在其完成后捕获编辑器辉光。
    [HarmonyPatch(typeof(PawnCacheRenderer), nameof(PawnCacheRenderer.OnPostRender))]
    internal static class Patch_BladebearerBloomPreview
    {
        //只有刀兵编辑器主动开启的预览会进入此处理。
        [HarmonyPostfix]
        private static void Postfix() { BladebearerBloomPreview.RenderActive(); }
    }
}
