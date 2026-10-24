using HarmonyLib;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//弓臂在活动瞄准和编辑预览中不能复用静态图集。
[HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
internal static class Patch_BowbearerCache
{
    //这里只读组件，不在并行准备阶段创建图像或推进状态。
    private static void Prefix(Pawn ___pawn, ref bool disableCache)
    {
        if (___pawn.TryGetComp<CompBowbearerShooting>() != null) disableCache = true;
    }
}
