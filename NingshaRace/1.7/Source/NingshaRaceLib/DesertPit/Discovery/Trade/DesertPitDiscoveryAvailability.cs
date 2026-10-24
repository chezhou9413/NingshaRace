using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.DesertPit.Discovery.Defs;
using NingshaRaceLib.DesertPit.Discovery.Items;
using NingshaRaceLib.DesertPit.Discovery.World;
using NingshaRaceLib.DesertPit.Utility;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.DesertPit.Discovery.Trade
{
    //类职责：在商队形成时检查玩家物品、探索位置和任务状态，决定是否保证投放坐标地图。
    internal static class DesertPitDiscoveryAvailability
    {
        //函数职责：检查所有地图与玩家世界运输容器，避免只检查当前殖民地造成重复投放。
        public static bool ShouldSupply()
        {
            if (CompUseEffect_DesertPitCoordinates.HasOngoingQuest)
                return false;
            List<Thing> contents = new List<Thing>();
            foreach (Map map in Find.Maps)
            {
                bool pit = DesertPitMapUtility.IsDesertPitMap(map)
                    || map.Parent is WorldObject_DesertPitDiscovery;
                foreach (Thing thing in map.listerThings.AllThings)
                {
                    if (thing is Pawn pawn && pawn.Faction == Faction.OfPlayer && !pawn.Dead && pit)
                        return false;
                    if (thing.def == DesertPitDiscoveryDefOf.NingshaRace_DesertPitCoordinateMap
                        && ((map.IsPlayerHome && !thing.Position.Fogged(map)) || thing.IsInAnyStorage()))
                        return false;
                    bool playerOwned = thing.Faction == Faction.OfPlayer;
                    //无派系的运输舱也可能持有玩家成员，先读取内容再判断所属。
                    if (!playerOwned && !(thing is IActiveTransporter) && !(thing is Skyfaller))
                        continue;
                    CollectContents(thing, contents);
                    if (!playerOwned)
                        playerOwned = contents.OfType<Pawn>().Any(member => member.Faction == Faction.OfPlayer);
                    if (playerOwned && BlocksSupply(contents, pit))
                        return false;
                }
            }
            foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
            {
                if (worldObject.Faction != Faction.OfPlayer || worldObject is MapParent
                    || !(worldObject is IThingHolder holder))
                    continue;
                ThingOwnerUtility.GetAllThingsRecursively(holder, contents);
                if (BlocksSupply(contents, false))
                    return false;
            }
            return true;
        }

        //函数职责：收集实体与组件中的实际物品，拒绝穿透非玩家成员的背包。
        private static void CollectContents(Thing thing, List<Thing> contents)
        {
            contents.Clear();
            List<IThingHolder> holders = new List<IThingHolder>();
            if (thing is IThingHolder holder)
                holders.Add(holder);
            if (thing is ThingWithComps withComps)
                holders.AddRange(withComps.AllComps.OfType<IThingHolder>());
            List<Thing> collected = new List<Thing>();
            foreach (IThingHolder root in holders)
            {
                ThingOwnerUtility.GetAllThingsRecursively(root, collected, false,
                    child => !(child is Pawn member) || member.Faction == Faction.OfPlayer);
                contents.AddRange(collected);
            }
        }

        //函数职责：判断容器内是否有坐标地图，或有正在巨坑内的玩家成员。
        private static bool BlocksSupply(List<Thing> things, bool pit)
        {
            return things.Any(thing => thing.def == DesertPitDiscoveryDefOf.NingshaRace_DesertPitCoordinateMap
                || (pit && thing is Pawn pawn && pawn.Faction == Faction.OfPlayer && !pawn.Dead));
        }
    }
}
