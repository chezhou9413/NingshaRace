using HarmonyLib;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Erosion.Components;

namespace NingshaRaceLib.Consumables.Patches
{
    //类职责：在炙热地煲确实被摄入后结算凝砂族侵蚀降低与一天心情效果。
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_ScorchingEarthStewIngested
    {
        //函数职责：只对实际产生营养的炙热地煲进食执行一次效果。
        public static void Postfix(Thing __instance, Pawn ingester, float __result)
        {
            if (__result <= 0f || __instance.def != DefOfRefs.NingshaRace_ScorchingEarthStew || ingester == null)
            {
                return;
            }
            ingester.TryGetComp<CompNingshaErosion>()?.ReduceErosion(10f);
            ingester.needs?.mood?.thoughts?.memories?.TryGainMemory(DefOfRefs.NingshaRace_AteScorchingEarthStew);
        }
    }
}
