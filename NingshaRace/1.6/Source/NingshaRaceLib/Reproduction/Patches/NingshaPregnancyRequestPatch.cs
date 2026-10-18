using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using Verse;

namespace NingshaRaceLib.Reproduction.Patches
{
    //生成请求同时排除原版复用已有孕妇的路径。
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    internal static class NingshaPregnancyRequestPatch
    {
        //在请求校验与世界人物挑选之前禁止带孕生成。
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ref PawnGenerationRequest request)
        {
            if (request.KindDef?.race == DefOfRefs.NingshaRace) request.AllowPregnant = false;
        }
    }
}
