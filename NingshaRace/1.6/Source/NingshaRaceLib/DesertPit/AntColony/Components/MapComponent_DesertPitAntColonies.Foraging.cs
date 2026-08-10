using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：集中实现工蚁全图物资缓存、实体储藏格分配和巢群食物消耗。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：扫描一次地图并缓存符合巢群搬运规则的食物、尸体和贵重品。
        private void RefreshForageCandidates()
        {
            forageCandidates.Clear();
            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing.Spawned && IsForageThing(thing) && !IsInAnyStorageCell(thing.Position))
                {
                    forageCandidates.Add(thing);
                }
            }
        }

        //函数职责：判断实体是否属于工蚁允许搬运的食物、新鲜尸体或指定贵重品。
        private static bool IsForageThing(Thing thing)
        {
            Corpse corpse = thing as Corpse;
            if (corpse != null)
            {
                return corpse.GetRotStage() == RotStage.Fresh;
            }

            if (thing.def.category == ThingCategory.Item && thing.def.IsNutritionGivingIngestible && !thing.def.IsDrug)
            {
                return true;
            }

            return thing.def == ThingDefOf.Silver ||
                   thing.def == ThingDefOf.Gold ||
                   thing.def == ThingDefOf.Jade ||
                   thing.def == ThingDefOf.ComponentIndustrial ||
                   thing.def == ThingDefOf.ComponentSpacer;
        }

        //函数职责：判断格子是否属于任意巢群的实体储藏范围。
        private bool IsInAnyStorageCell(IntVec3 cell)
        {
            for (int i = 0; i < colonies.Count; i++)
            {
                if (colonies[i].StorageCells.Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：为工蚁选择最近可达物资和可用储藏格，并创建实体搬运工作。
        private Job TryCreateForageJob(Pawn pawn, AntColonyState state)
        {
            Comp_DesertPitAntMember memberComp = pawn.TryGetComp<Comp_DesertPitAntMember>();
            if (memberComp == null || !memberComp.CanStartForage(Find.TickManager.TicksGame, Settings.workerHaulLimit))
            {
                return null;
            }

            Thing bestThing = null;
            IntVec3 bestCell = IntVec3.Invalid;
            int bestCount = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < forageCandidates.Count; i++)
            {
                Thing candidate = forageCandidates[i];
                if (candidate == null || !candidate.Spawned || assignedForageThings.ContainsKey(candidate))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(candidate, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    continue;
                }

                IntVec3 storageCell;
                int carryCount;
                if (!TryFindStorageCell(pawn, state, candidate, out storageCell, out carryCount))
                {
                    continue;
                }

                float distance = pawn.Position.DistanceToSquared(candidate.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestThing = candidate;
                    bestCell = storageCell;
                    bestCount = carryCount;
                }
            }

            if (bestThing == null)
            {
                return null;
            }

            assignedForageThings[bestThing] = pawn;
            assignedStorageCells[bestCell] = pawn;
            Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_DesertPitAntHaul, bestThing, bestCell);
            job.count = bestCount;
            job.haulMode = HaulMode.ToCellNonStorage;
            return job;
        }

        //函数职责：优先寻找可合并同类堆叠的储藏格，其次寻找完全空闲的储藏格。
        private bool TryFindStorageCell(Pawn pawn, AntColonyState state, Thing thing, out IntVec3 cell, out int carryCount)
        {
            int pawnCapacity = pawn.carryTracker.MaxStackSpaceEver(thing.def);
            for (int i = 0; i < state.StorageCells.Count; i++)
            {
                IntVec3 candidate = state.StorageCells[i];
                Thing occupant = GetStorageOccupant(candidate);
                if (occupant == null || occupant.def != thing.def || occupant is Corpse || occupant.stackCount >= occupant.def.stackLimit)
                {
                    continue;
                }

                if (assignedStorageCells.ContainsKey(candidate) || !pawn.CanReserveAndReach(candidate, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                cell = candidate;
                carryCount = System.Math.Min(System.Math.Min(thing.stackCount, pawnCapacity), occupant.def.stackLimit - occupant.stackCount);
                return carryCount > 0;
            }

            for (int i = 0; i < state.StorageCells.Count; i++)
            {
                IntVec3 candidate = state.StorageCells[i];
                if (GetStorageOccupant(candidate) != null || assignedStorageCells.ContainsKey(candidate))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(candidate, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                cell = candidate;
                carryCount = System.Math.Min(thing.stackCount, pawnCapacity);
                return carryCount > 0;
            }

            cell = IntVec3.Invalid;
            carryCount = 0;
            return false;
        }

        //函数职责：取得储藏格中决定该格物资类型的实体堆叠或尸体。
        private Thing GetStorageOccupant(IntVec3 cell)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing is Corpse || thing.def.category == ThingCategory.Item)
                {
                    return thing;
                }
            }

            return null;
        }

        //函数职责：验证搬运工作抵达前目标储藏格仍允许放入指定物资。
        public bool IsStorageCellAvailableFor(Pawn pawn, IntVec3 cell, Thing thing)
        {
            Pawn assignedPawn;
            if (assignedStorageCells.TryGetValue(cell, out assignedPawn) && assignedPawn != pawn)
            {
                return false;
            }

            Thing occupant = GetStorageOccupant(cell);
            return occupant == null || (!(thing is Corpse) && !(occupant is Corpse) && occupant.def == thing.def && occupant.stackCount < occupant.def.stackLimit);
        }

        //函数职责：释放指定工蚁持有的物资和储藏格分配。
        public void ReleaseForageAssignments(Pawn pawn)
        {
            List<Thing> thingsToRemove = new List<Thing>();
            foreach (KeyValuePair<Thing, Pawn> pair in assignedForageThings)
            {
                if (pair.Value == pawn)
                {
                    thingsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < thingsToRemove.Count; i++)
            {
                assignedForageThings.Remove(thingsToRemove[i]);
            }

            List<IntVec3> cellsToRemove = new List<IntVec3>();
            foreach (KeyValuePair<IntVec3, Pawn> pair in assignedStorageCells)
            {
                if (pair.Value == pawn)
                {
                    cellsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < cellsToRemove.Count; i++)
            {
                assignedStorageCells.Remove(cellsToRemove[i]);
            }
        }

        //函数职责：在工蚁成功放入物资后累计本轮搬运次数并释放任务分配。
        public void NotifyForageCompleted(Pawn pawn)
        {
            Comp_DesertPitAntMember memberComp = pawn?.TryGetComp<Comp_DesertPitAntMember>();
            if (memberComp != null)
            {
                memberComp.NotifySuccessfulHaul(Find.TickManager.TicksGame, Settings.workerHaulLimit, Settings.workerHaulCooldownTicks);
            }

            ReleaseForageAssignments(pawn);
        }

        //函数职责：供其他地图生态系统查询指定格子是否属于任意蚁巢储藏区。
        public bool IsColonyStorageCell(IntVec3 cell)
        {
            return IsInAnyStorageCell(cell);
        }

        //函数职责：定期释放已经不再执行对应搬运工作的运行时分配。
        private void ReleaseStaleForageAssignments()
        {
            HashSet<Pawn> assignedPawns = new HashSet<Pawn>();
            foreach (Pawn pawn in assignedForageThings.Values)
            {
                assignedPawns.Add(pawn);
            }

            foreach (Pawn pawn in assignedStorageCells.Values)
            {
                assignedPawns.Add(pawn);
            }

            foreach (Pawn pawn in assignedPawns)
            {
                if (pawn == null || pawn.Destroyed || pawn.CurJobDef != DefOfRefs.NingshaRace_Job_DesertPitAntHaul)
                {
                    ReleaseForageAssignments(pawn);
                }
            }
        }

    }
}
