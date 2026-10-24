using HarmonyLib;
using NingshaRaceLib.Erosion.Utility;
using Verse;

namespace NingshaRaceLib.Erosion.Rendering
{
    //活体黑雾与凝砂蛇头每帧更新，避免远景人物图集把动画固定成单帧。
    [HarmonyPatch(typeof(PawnRenderer), "PawnNeedsHediffMaterial")]
    public static class Patch_ErosionSnakeCache
    {
        //只让存活侵蚀体继续使用渲染树，不改变隐身标记和其他 Pawn。
        [HarmonyPostfix]
        public static void Postfix(Pawn ___pawn, ref bool __result)
        {
            if (!__result && !___pawn.Dead && ErosionPawnUtility.IsErosionBody(___pawn)) __result = true;
        }
    }
}
