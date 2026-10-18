using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using Verse;

namespace NingshaRaceLib.Reproduction.Patches
{
    //生成凝砂族时关闭初始怀孕，不改变正常繁殖和产卵流程。
    [HarmonyPatch(typeof(PawnGenerator), "GenerateInitialHediffs")]
    internal static class NingshaGeneratedPregnancyPatch
    {
        //按最终生成种族判断，覆盖其他模组替换 PawnKind 的情况。
        private static void Prefix(Pawn pawn, ref PawnGenerationRequest request)
        {
            if (pawn.def == DefOfRefs.NingshaRace) request.AllowPregnant = false;
        }
    }
}
