using HarmonyLib;
using NingshaRaceLib.SandGolem.Lifecycle;
using Verse;

namespace NingshaRaceLib.SandGolem.Patches
{
    //类职责：将外部事件误选的沙傀静默替换为普通机械体。
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
    public static class SandGolemGenerationPatch
    {
        //函数职责：在生成实体之前调整误选请求，继续执行原版生成流程。
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ref PawnGenerationRequest request)
        {
            SandGolemGenerationGate.ResolveRequest(ref request);
        }
    }
}
