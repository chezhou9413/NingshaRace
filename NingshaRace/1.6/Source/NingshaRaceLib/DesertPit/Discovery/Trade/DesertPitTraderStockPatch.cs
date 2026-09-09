using System;
using System.Linq;
using HarmonyLib;
using NingshaRaceLib.DesertPit.Discovery.Defs;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.Discovery.Trade
{
    //类职责：在来访贸易群组创建完成时投放一次真实坐标地图库存。
    [HarmonyPatch(typeof(LordMaker), nameof(LordMaker.MakeNewLord))]
    internal static class DesertPitTraderStockPatch
    {
        //函数职责：确认来访殖民地商队满足投放条件，并把地图放入载货成员背包。
        private static void Postfix(Lord __result, Map map)
        {
            if (__result == null || !(__result.LordJob is LordJob_TradeWithColony)
                || !map.IsPlayerHome || __result.faction == null || __result.faction == Faction.OfPlayer
                || __result.faction.HostileTo(Faction.OfPlayer))
                return;
            Pawn trader = __result.ownedPawns.FirstOrDefault(pawn => pawn.TraderKind != null);
            if (trader == null || !DesertPitDiscoveryAvailability.ShouldSupply())
                return;
            ThingDef mapDef = DesertPitDiscoveryDefOf.NingshaRace_DesertPitCoordinateMap;
            if (__result.ownedPawns.Any(pawn => pawn.inventory != null
                && pawn.inventory.innerContainer.Any(thing => thing.def == mapDef)))
                return;
            Pawn carrier = __result.ownedPawns.FirstOrDefault(pawn => pawn.inventory != null
                && pawn.GetTraderCaravanRole() == TraderCaravanRole.Carrier) ?? trader;
            Thing coordinateMap = ThingMaker.MakeThing(mapDef);
            if (!carrier.inventory.innerContainer.TryAdd(coordinateMap))
            {
                coordinateMap.Destroy();
                throw new InvalidOperationException("来访商队无法携带巨坑坐标地图。");
            }
        }
    }
}
