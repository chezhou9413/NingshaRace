using System;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;

namespace NingshaRaceLib.Scenarios.Generation
{
    //类职责：在地表沙漠安置开局巨坑入口，保留完整通行外圈及通往地图边缘的道路。
    internal static class NingshaStartingGatePlacement
    {
        //函数职责：优先在地表预定落点附近选择空地，没有合适位置时明确报告失败。
        public static Building_DesertPitGate Spawn(Map map)
        {
            IntVec3 center = MapGenerator.PlayerStartSpot;
            Predicate<IntVec3> validator = candidate => CanPlace(map, candidate);
            if (!CellFinder.TryFindRandomCellNear(center, map, 40, validator, out IntVec3 cell)
                && !CellFinder.TryFindRandomCell(map, validator, out cell))
                throw new InvalidOperationException("地表沙漠没有能安置巨坑入口且通向地图边缘的空地。");
            Building_DesertPitGate gate = (Building_DesertPitGate)GenSpawn.Spawn(
                ThingMaker.MakeThing(DefOfRefs.NingshaRace_DesertPitGate), cell, map);
            foreach (IntVec3 adjacent in gate.OccupiedRect().ExpandedBy(1))
                map.fogGrid.Unfog(adjacent);
            return gate;
        }

        //函数职责：检查入口完整占地及外侧通行圈，不覆盖既有建筑、深水或厚岩顶。
        private static bool CanPlace(Map map, IntVec3 center)
        {
            CellRect footprint = GenAdj.OccupiedRect(center, Rot4.North, DefOfRefs.NingshaRace_DesertPitGate.size);
            foreach (IntVec3 cell in footprint.ExpandedBy(2))
            {
                if (!cell.InBounds(map) || !cell.Standable(map) || cell.Roofed(map)
                    || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null
                    || !cell.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Heavy))
                    return false;
            }
            return map.reachability.CanReachMapEdge(center, TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater));
        }
    }
}
