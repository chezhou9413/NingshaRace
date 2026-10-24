using System;
using NingshaRaceLib.Core.Defs;
using Verse;

namespace NingshaRaceLib.SandGolem.Lifecycle
{
    //类职责：识别召唤工厂的单次许可，并把外部沙傀请求转换为普通机械体请求。
    internal static class SandGolemGenerationGate
    {
        [ThreadStatic] private static PawnKindDef permittedKind;

        //函数职责：授权本次召唤生成，并在成功或异常退出时恢复调用上下文。
        public static Pawn GenerateForSummon(PawnGenerationRequest request)
        {
            PawnKindDef previous = permittedKind;
            permittedKind = request.KindDef;
            try
            {
                return PawnGenerator.GeneratePawn(request);
            }
            finally
            {
                permittedKind = previous;
            }
        }

        //函数职责：放行正常召唤，外部误选则原地替换请求种类，让原事件继续生成。
        public static void ResolveRequest(ref PawnGenerationRequest request)
        {
            PawnKindDef kind = request.KindDef;
            if (kind?.race != DefOfRefs.NingshaRace_SandGolem) return;
            if (permittedKind != kind)
            {
                PawnKindDef replacement = SandGolemReplacementSelector.Select(kind);
                //动态种类回调优先于 KindDef，必须一同解除以防再次返回沙傀。
                request.PawnKindDefGetter = null;
                request.KindDef = replacement;
                return;
            }
            //进入原版生成流程前消耗许可，避免其中嵌套的事件继续生成额外沙傀。
            permittedKind = null;
        }
    }
}
