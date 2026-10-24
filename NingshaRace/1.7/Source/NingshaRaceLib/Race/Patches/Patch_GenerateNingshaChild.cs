using HarmonyLib;
using Verse;

using NingshaRaceLib.Race.Generation;

namespace NingshaRaceLib.Race.Patches
{
    //类职责：在统一人物生成入口准备凝砂儿童请求，不接管原版人物创建与成长流程。
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    internal static class Patch_GenerateNingshaChild
    {
        //职责：在原版请求校验前协调儿童阶段与年龄，覆盖调试菜单和直接请求两种入口。
        private static void Prefix(ref PawnGenerationRequest request)
        {
            NingshaChildGenerationUtility.PrepareRequest(ref request);
        }
    }
}
