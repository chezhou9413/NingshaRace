using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Scenarios.Generation
{
    //类职责：在巨坑离洞绳附近直接安置开局队伍与物资，避免空投对厚岩顶和迷雾的限制。
    internal static class NingshaStartingPartyPlacement
    {
        private const float LandingRadius = 6f;

        //函数职责：整理连通安全区，安置每位成员及场景物资，并核对成员能够步行到离洞绳。
        public static void Place(Map map, PocketMapExit exit)
        {
            List<Pawn> pawns = Find.GameInitData.startingAndOptionalPawns;
            foreach (Pawn pawn in pawns)
            {
                if (pawn.Spawned || pawn.Dead || pawn.def != DefOfRefs.NingshaRace || pawn.Faction != Faction.OfPlayer)
                    throw new InvalidOperationException($"开局成员 {pawn.LabelShort} 的状态不允许在巨坑安置。");
            }

            List<IntVec3> cells = PrepareLandingArea(map, exit);
            HashSet<IntVec3> landing = new HashSet<IntVec3>(cells);
            int nextCell = 0;
            foreach (Pawn pawn in pawns)
            {
                while (nextCell < cells.Count && (cells[nextCell].GetFirstPawn(map) != null
                    || cells[nextCell].GetItemCount(map) != 0)) nextCell++;
                if (nextCell >= cells.Count)
                    throw new InvalidOperationException("离洞绳附近没有足够的开局成员站立位置。");
                GenSpawn.Spawn(pawn, cells[nextCell++], map, Rot4.South);
                if (!pawn.CanReach(exit, PathEndMode.Touch, Danger.Deadly))
                    throw new InvalidOperationException($"开局成员 {pawn.LabelShort} 无法走到离洞绳。");
            }

            foreach (ScenPart part in Find.Scenario.AllParts)
                foreach (Thing thing in part.PlayerStartingThings())
                    PlaceStartingThing(thing, map, exit.Position, landing);
            foreach (Pawn pawn in pawns)
                foreach (ThingDefCount possession in Find.GameInitData.startingPossessions[pawn])
                    PlaceStartingThing(StartingPawnUtility.GenerateStartingPossession(possession), map, exit.Position, landing);

            foreach (IntVec3 cell in cells)
            {
                map.fogGrid.Unfog(cell);
                map.areaManager.Home[cell] = true;
            }
            map.fogGrid.Unfog(exit.Position);
        }

        //函数职责：只清理出口附近的阻挡物，保留绳索、生物和既有物资，返回与出口实际连通的格子。
        private static List<IntVec3> PrepareLandingArea(Map map, PocketMapExit exit)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(exit.Position, LandingRadius, true))
            {
                if (!cell.InBounds(map)) continue;
                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (thing == exit || thing is Pawn || thing.def.passability != Traversability.Impassable) continue;
                    if (!thing.def.destroyable || thing is MapPortal)
                        throw new InvalidOperationException($"巨坑开局安全区被不可清理的 {thing.Label} 阻挡。");
                    thing.Destroy(DestroyMode.Vanish);
                }
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Sand);
            }
            map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();

            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(exit.Position, LandingRadius, true))
            {
                if (cell.InBounds(map) && !exit.OccupiedRect().Contains(cell)
                    && cell.Standable(map) && cell.GetEdifice(map) == null
                    && map.reachability.CanReach(cell, exit, PathEndMode.Touch,
                        TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater)))
                    result.Add(cell);
            }
            return result;
        }

        //函数职责：拆分超量堆叠并限制物资只落在已确认连通的安全区，不散落到隔墙或地图边缘。
        private static void PlaceStartingThing(Thing thing, Map map, IntVec3 center, HashSet<IntVec3> landing)
        {
            if (thing.def.CanHaveFaction) thing.SetFactionDirect(Faction.OfPlayer);
            thing.SetForbidden(false, false);
            //原版场景可一次返回数百件同类物资，逐堆投放才能在足够的空格内完整保留数量。
            while (thing.stackCount > thing.def.stackLimit)
                PlaceStack(thing.SplitOff(thing.def.stackLimit), map, center, landing);
            PlaceStack(thing, map, center, landing);
        }

        //函数职责：放置一个合法堆叠，失败时输出明确错误而不悄悄丢失开局物品。
        private static void PlaceStack(Thing thing, Map map, IntVec3 center, HashSet<IntVec3> landing)
        {
            if (!GenPlace.TryPlaceThing(thing, center, map, ThingPlaceMode.Near,
                extraValidator: cell => landing.Contains(cell) && cell.GetFirstPawn(map) == null))
                throw new InvalidOperationException($"开局物品 {thing.Label} 无法放入离洞绳附近的安全区。");
        }
    }
}
