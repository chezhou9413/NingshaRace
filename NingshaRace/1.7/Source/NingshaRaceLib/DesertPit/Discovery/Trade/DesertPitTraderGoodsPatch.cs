using System.Collections.Generic;
using HarmonyLib;
using NingshaRaceLib.DesertPit.Discovery.Defs;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.Discovery.Trade
{
    //类职责：让没有驮兽的来访商队也能出售保存在商人背包内的坐标地图。
    [HarmonyPatch(typeof(Pawn_TraderTracker), "Goods", MethodType.Getter)]
    internal static class DesertPitTraderGoodsPatch
    {
        //函数职责：只补充原版货物枚举遗漏的实际地图，不生成或补货。
        private static void Postfix(Pawn ___pawn, ref IEnumerable<Thing> __result)
        {
            if (___pawn.GetLord()?.LordJob is LordJob_TradeWithColony)
                __result = IncludeCoordinates(__result, ___pawn);
        }

        //函数职责：保留原版货物顺序，并避免坐标地图在交易窗口重复出现。
        private static IEnumerable<Thing> IncludeCoordinates(IEnumerable<Thing> goods, Pawn trader)
        {
            HashSet<Thing> seen = new HashSet<Thing>();
            foreach (Thing thing in goods)
            {
                seen.Add(thing);
                yield return thing;
            }
            foreach (Thing thing in trader.inventory.innerContainer)
            {
                if (thing.def == DesertPitDiscoveryDefOf.NingshaRace_DesertPitCoordinateMap && seen.Add(thing))
                    yield return thing;
            }
        }
    }
}
