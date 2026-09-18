using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.SandGolem.Rendering
{
    //类职责：只在沙傀截图期间提供站立全身参数，不改变施法者真实状态。
    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public sealed class SandGolemCaptureScope : IDisposable
    {
        [ThreadStatic] private static Pawn capturePawn;
        private readonly Pawn previous;

        //函数职责：保存嵌套截图上下文并指定本次截图角色。
        public SandGolemCaptureScope(Pawn pawn)
        {
            previous = capturePawn;
            capturePawn = pawn;
        }

        //函数职责：在正常结束和异常退出时恢复截图上下文。
        public void Dispose()
        {
            capturePawn = previous;
        }

        //函数职责：让截图始终绘制站立的完整身体及服装。
        private static void Postfix(ref PawnDrawParms __result)
        {
            if (capturePawn == null || __result.pawn != capturePawn) return;
            __result.posture = PawnPosture.Standing;
            __result.bed = null;
            __result.crawling = false;
            __result.swimming = false;
            __result.carriedThing = null;
            __result.flags &= ~PawnRenderFlags.NoBody;
        }
    }
}
