using HarmonyLib;
using NingshaRaceLib.DesertPit.Discovery.Defs;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Discovery.Trade
{
    //类职责：允许地面商队交易坐标地图，不向任何随机库存生成器添加物品。
    [HarmonyPatch(typeof(TraderKindDef), nameof(TraderKindDef.WillTrade))]
    internal static class DesertPitTraderAcceptancePatch
    {
        //函数职责：让地面贸易类型认可实际携带的坐标地图。
        private static void Postfix(TraderKindDef __instance, ThingDef td, ref bool __result)
        {
            if (!__instance.orbital && td == DesertPitDiscoveryDefOf.NingshaRace_DesertPitCoordinateMap)
                __result = true;
        }
    }
}
