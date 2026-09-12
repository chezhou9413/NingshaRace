using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.AI;
using NingshaRaceLib.DesertPit.Antlion.Components;
using NingshaRaceLib.DesertPit.Antlion.Holders;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Generation
{
    //类职责：统计地表与地下存活蚁狮，并为种群补充寻找远离聚落和生物的沙地。
    internal static class AntlionPopulationUtility
    {
        //函数职责：统计地表蚁狮和地下容器中的存活个体，记录其位置以限制补充间距。
        public static int CollectPopulation(Map map, List<IntVec3> positions)
        {
            positions.Clear();
            int count = 0;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.def != DefOfRefs.NingshaRace_Antlion || pawn.Dead) continue;
                count++;
                positions.Add(pawn.Position);
            }
            foreach (Thing thing in map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_AntlionBurrow))
            {
                var burrow = (AntlionBurrow)thing;
                foreach (Thing held in burrow.GetDirectlyHeldThings())
                {
                    Pawn pawn = held as Pawn;
                    if (pawn == null || pawn.Dead || pawn.def != DefOfRefs.NingshaRace_Antlion) continue;
                    count++;
                    positions.Add(burrow.Position);
                }
            }
            return count;
        }

        //函数职责：以有限随机尝试查找空闲沙地，不占用家园、入口、蚁巢或现有生物周围区域。
        public static IntVec3 FindReplenishmentCell(Map map, DefModExtension_DesertPitAntlion settings,
            CompProperties_AntlionAmbush behavior, List<IntVec3> positions)
        {
            List<Thing> exits = map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_DesertPitCaveExit);
            List<Thing> nests = map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_DesertPitAntNest);
            for (int attempt = 0; attempt < settings.replenishPlacementAttempts; attempt++)
            {
                IntVec3 cell = new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
                if (!AntlionSandUtility.CanBurrowAt(map, cell, behavior) || map.areaManager.Home[cell]) continue;
                if (NearThings(cell, exits, settings.entranceAvoidRadius)
                    || NearThings(cell, nests, settings.antNestAvoidRadius)) continue;
                bool blocked = false;
                foreach (IntVec3 position in positions)
                {
                    if (cell.DistanceToSquared(position) >= settings.spacing * settings.spacing) continue;
                    blocked = true;
                    break;
                }
                if (blocked) continue;
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (cell.DistanceToSquared(pawn.Position) >= settings.replenishPawnAvoidRadius * settings.replenishPawnAvoidRadius) continue;
                    blocked = true;
                    break;
                }
                if (!blocked) return cell;
            }
            return IntVec3.Invalid;
        }

        //函数职责：判定候选格是否落在指定实体的避让半径内。
        private static bool NearThings(IntVec3 cell, List<Thing> things, float radius)
        {
            foreach (Thing thing in things)
                if (cell.DistanceToSquared(thing.Position) < radius * radius) return true;
            return false;
        }
    }
}
