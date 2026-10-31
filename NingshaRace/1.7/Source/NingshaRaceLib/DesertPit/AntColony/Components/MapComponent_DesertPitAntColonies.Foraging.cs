using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.State;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //共享全图物资缓存，任务自身保存来源和目的地预留。
    public partial class MapComponent_DesertPitAntColonies
    {
        //只每三百 tick 扫描一次地图物品，不采收活植物。
        private void RefreshForageCandidates()
        {
            forageCandidates.Clear();
            if (colonies.Count == 0) return;
            MapComponent_AntHabitats habitats = map.GetComponent<MapComponent_AntHabitats>();
            foreach (Thing thing in map.listerThings.AllThings)
                if (thing.Spawned && IsForageThing(thing) && !IsColonyStorageCell(thing.Position)
                    && !habitats.IsRepelled(thing.Position)) forageCandidates.Add(thing);
        }

        //天然石块也进入缓存，只在巢群缺少建材时安排搬运。
        private static bool IsForageThing(Thing thing) => IsStoredFood(thing) || IsStoneChunk(thing)
            || thing.def == ThingDefOf.Steel || thing.def == ThingDefOf.WoodLog || thing.def == ThingDefOf.Silver
            || thing.def == ThingDefOf.Gold || thing.def == ThingDefOf.Jade
            || thing.def == ThingDefOf.ComponentIndustrial || thing.def == ThingDefOf.ComponentSpacer;

        //所有巢群的地面储藏格都从外出搬运缓存排除。
        public bool IsColonyStorageCell(IntVec3 cell) => colonies.Any(state => state.StorageCells.Contains(cell));

        //取格子中实际占用容量的物品或尸体。
        private Thing GetStorageOccupant(IntVec3 cell) => cell.GetThingList(map)
            .FirstOrDefault(thing => thing is Corpse || thing.def.category == ThingCategory.Item);

        //同一物资只允许一份搬运任务，覆盖不同巢群间的竞争。
        private bool HaulReserved(Thing thing) => colonies.Any(state => state.Tasks.Any(task => task.Kind == AntTaskKind.Haul && task.Target == thing));

        //优先蜜露收集，普通物资按八十、一百六十、二百四十格分层。
        private void PlanHauling(AntColonyState state)
        {
            bool foodOnly = GetStoredNutrition(state) < GetFoodReserve(state) * 2f;
            state.Tasks.RemoveAll(task => task.Kind == AntTaskKind.Haul && task.Worker == null
                && !task.FreeHaul && (task.Target == null || task.Target.Destroyed || foodOnly && !IsStoredFood(task.Target)
                    || !CanHaulTo(state, task.Target, task.Destination, task.Cell)));
            int budget = Math.Max(1, state.Population.WorkerTarget) - state.Tasks.Count(task => task.Kind == AntTaskKind.Haul);
            if (budget <= 0) return;
            List<Thing> candidates = new List<Thing>();
            foreach (Building_AntHoneyTower tower in state.Buildings.OfType<Building_AntHoneyTower>()) candidates.AddRange(tower.Contents);
            candidates.AddRange(forageCandidates);
            bool needsStone = state.Tasks.Any(task => task.Kind == AntTaskKind.Build && task.Worker == null)
                && StoredStone(state).Sum(thing => thing.stackCount) < 8;
            foreach (Thing thing in candidates.OrderBy(thing => HaulPriority(state, thing, foodOnly)))
            {
                if (budget <= 0) break;
                if (thing.Destroyed || HaulReserved(thing) || !thing.Spawned && !(thing.ParentHolder is Building_AntHoneyTower)) continue;
                if (thing.Spawned && IsColonyStorageCell(thing.Position)) continue;
                bool food = IsStoredFood(thing);
                if (foodOnly && !food || !food && IsStoneChunk(thing) && !needsStone) continue;
                if (!foodOnly && thing.PositionHeld.DistanceTo(state.NestPosition) > 240f) continue;
                if (state.WorkerDanger.Contains(state, Settings, thing.PositionHeld, Find.TickManager.TicksGame)) continue;
                if (TryPlanHaul(state, thing, false, HaulPriority(state, thing, foodOnly))) budget--;
            }
        }

        //距离以蚁巢为中心，缺粮时食物不受距离上限限制。
        private int HaulPriority(AntColonyState state, Thing thing, bool foodOnly)
        {
            if (foodOnly) return 5;
            if (thing.ParentHolder is Building_AntHoneyTower) return 8;
            int band = Math.Min(2, (int)(Math.Max(0f, thing.PositionHeld.DistanceTo(state.NestPosition) - 0.001f) / 80f));
            if (IsStoneChunk(thing)) return 65;
            return 80 + band * 2 + (IsStoredFood(thing) ? 0 : 1);
        }

        //规划来源与目的地，不移动实物；取货失败时只释放记录。
        private bool TryPlanHaul(AntColonyState state, Thing thing, bool free, int priority, Pawn preferredWorker = null)
        {
            if (!TryFindHaulDestination(state, thing, out Thing destination, out IntVec3 cell, out int count)) return false;
            if (!CanHaulTo(state, thing, destination, cell)) return false;
            state.Tasks.Add(new AntColonyTask { Kind = AntTaskKind.Haul, Target = thing, Destination = destination,
                Cell = cell, Count = count, FreeHaul = free, Priority = priority, PreferredWorker = preferredWorker });
            return true;
        }

        //只排至少有一只工蚁能到达来源和目的地的搬运，封墙或满仓不会堵死列表。
        private bool CanHaulTo(AntColonyState state, Thing source, Thing destination, IntVec3 cell)
        {
            if (destination is Building_AntStoragePit pit && (!pit.Spawned || pit.SpaceFor(source) <= 0)) return false;
            if (destination == null && !IsStorageCellAvailableFor(null, cell, source)) return false;
            Thing pickup = source.Spawned ? source : source.ParentHolder as Thing;
            if (pickup == null || !pickup.Spawned) return false;
            return state.Members.Any(worker => worker.Spawned && !worker.Dead && !worker.Downed
                && worker.GetComp<Comp_DesertPitAntMember>().Caste == Core.AntCaste.Worker
                && worker.CanReach(pickup, PathEndMode.Touch, Danger.Deadly)
                && worker.CanReach(destination != null ? new LocalTargetInfo(destination) : new LocalTargetInfo(cell),
                    destination != null ? PathEndMode.Touch : PathEndMode.OnCell, Danger.Deadly));
        }

        //先维持四个地面食物格，再使用粮仓和其他库位。
        private bool TryFindHaulDestination(AntColonyState state, Thing thing, out Thing destination, out IntVec3 cell, out int count)
        {
            destination = null;
            cell = IntVec3.Invalid;
            count = 0;
            bool food = IsStoredFood(thing);
            if (food && FindGroundDestination(state, thing, true, out cell, out count)) return true;
            foreach (Building_AntStoragePit pit in state.Buildings.OfType<Building_AntStoragePit>().OrderByDescending(pit => pit.FoodOnly))
            {
                if (pit.FoodOnly && !food || !pit.Spawned || state.Tasks.Any(task => task.Destination == pit)) continue;
                count = Math.Min(thing.stackCount, pit.SpaceFor(thing));
                if (count <= 0) continue;
                destination = pit;
                cell = pit.Position;
                return true;
            }
            return FindGroundDestination(state, thing, false, out cell, out count);
        }

        //每格只预留给一份任务，尸体占完整一格。
        private bool FindGroundDestination(AntColonyState state, Thing thing, bool foodSlots, out IntVec3 result, out int count)
        {
            result = IntVec3.Invalid;
            count = 0;
            for (int i = 0; i < state.StorageCells.Count; i++)
            {
                if ((i < Settings.foodStorageCells) != foodSlots) continue;
                IntVec3 cell = state.StorageCells[i];
                if (!cell.Standable(map) || state.Tasks.Any(task => task.Kind == AntTaskKind.Haul && task.Destination == null && task.Cell == cell)) continue;
                Thing occupant = GetStorageOccupant(cell);
                if (occupant != null && (!occupant.CanStackWith(thing) || occupant.stackCount >= occupant.def.stackLimit)) continue;
                result = cell;
                count = Math.Min(thing.stackCount, thing.def.stackLimit - (occupant?.stackCount ?? 0));
                return count > 0;
            }
            return false;
        }

        //抵达前再次检查容量，外部玩家搬运不会导致覆盖库存。
        public bool IsStorageCellAvailableFor(Pawn pawn, IntVec3 cell, Thing thing)
        {
            Thing occupant = GetStorageOccupant(cell);
            return occupant == null || occupant.CanStackWith(thing) && occupant.stackCount < occupant.def.stackLimit;
        }
    }
}
