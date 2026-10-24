using HarmonyLib;
using Verse;

using NingshaRaceLib.Molting.Components;

namespace NingshaRaceLib.Molting.Patches
{
    //类职责：把原版Thing.Ingested返回的实际营养等量计入凝砂族蜕皮组件。
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_ThingIngestedMoltingNutrition
    {
        //函数职责：在进食成功后使用原版最终返回值累计营养，避免按物品标称值重复计算。
        public static void Postfix(Pawn ingester, float __result)
        {
            ingester?.TryGetComp<CompNingshaMolting>()?.AddIngestedNutrition(__result);
        }
    }
}
