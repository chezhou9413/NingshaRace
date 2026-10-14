using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //在原版更新阶段接入当前引擎的地图渲染入口。
    [HarmonyPatch(typeof(Root_Play), nameof(Root_Play.Update))]
    internal static class Patch_BladebearerBloomCamera
    {
        //地图后处理在渲染管线执行，不在界面阶段截屏。
        [HarmonyPostfix]
        private static void Postfix() { BladebearerBloomCamera.EnsureAttached(); }
    }

#if RW_1_7
    //原生相机先收集网格，再交给烘焙器实际绘制。
    [HarmonyPatch(typeof(RimWorldWaylith.Rendering.CameraTextureBaker), nameof(RimWorldWaylith.Rendering.CameraTextureBaker.Render))]
    internal static class Patch_BladebearerBloomPreview
    {
        //只替换刀兵编辑器开启的独占预览，其他头像继续走原版烘焙。
        [HarmonyPrefix]
        private static bool Prefix(UnityEngine.RenderTexture target,
            System.Collections.Generic.IReadOnlyList<Waylith.Rendering.MeshDrawCommand> commands,
            Waylith.Rendering.CameraView view, UnityEngine.Rect viewportRect, UnityEngine.Color? clearColor)
            => !BladebearerBloomPreview.RenderNative(target, commands, view, viewportRect, clearColor);
    }
#else
    //Pawn 相机在 OnPostRender 中即时画角色，必须在其完成后捕获编辑器辉光。
    [HarmonyPatch(typeof(PawnCacheRenderer), nameof(PawnCacheRenderer.OnPostRender))]
    internal static class Patch_BladebearerBloomPreview
    {
        //只有刀兵编辑器主动开启的预览会进入此处理。
        [HarmonyPostfix]
        private static void Postfix() { BladebearerBloomPreview.RenderActive(); }
    }
#endif
}
