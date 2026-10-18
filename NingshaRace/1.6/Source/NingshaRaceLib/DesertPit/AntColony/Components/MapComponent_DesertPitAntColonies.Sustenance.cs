using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using NingshaRaceLib.DesertPit.Ecology.Plants;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：维护食用菌采收缓存和巢群口粮底线，连接自然生长、采收、搬运与进食。
    public partial class MapComponent_DesertPitAntColonies
    {
        private readonly Dictionary<int, List<Plant>> harvestCandidates = new Dictionary<int, List<Plant>>();

        //按地图当地时间安排下一次十二点检查。
        private int NextLocalNoonTick(int now)
        {
            int remaining = GenDate.TicksPerDay / 2 - GenLocalDate.DayTick(map);
            if (remaining <= 0) remaining += GenDate.TicksPerDay;
            return now + remaining;
        }

        //正午缺粮时给全巢工蚁下发采菇指令，达到储备目标后结束指令。
        private void UpdateHarvestOrder(AntColonyState state, int now)
        {
            if (state.NestDestroyed)
            {
                state.HarvestRequested = false;
                return;
            }
            float stored = GetStoredNutrition(state);
            float reserve = GetFoodReserve(state);
            bool lacksFood = stored < reserve * Settings.harvestReserveMultiplier;
            if (!lacksFood) state.HarvestRequested = false;
            //口粮低于基本储备时立即补粮，正常储备仍按正午计划采收。
            if (now < state.NextHarvestOrderTick && (stored >= reserve || state.HarvestRequested)) return;
            state.NextHarvestOrderTick = NextLocalNoonTick(now);
            state.HarvestRequested = lacksFood;
            if (!lacksFood) return;
            RefreshHarvestCandidates();
            foreach (Pawn worker in state.Members)
            {
                if (!worker.Spawned || worker.Dead || worker.Downed || worker.InMentalState
                    || worker.TryGetComp<Comp_DesertPitAntMember>().Caste != AntCaste.Worker
                    || worker.carryTracker.CarriedThing != null) continue;
                JobDef current = worker.CurJobDef;
                if (current == JobDefOf.Goto || current == JobDefOf.Wait
                    || current == DefOfRefs.NingshaRace_Job_DesertPitAntHaul
                    || current == DefOfRefs.NingshaRace_Job_DesertPitAntDigResource)
                    worker.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            }
        }

        //函数职责：按照现有存活成员实际每日饥饿消耗预留口粮，升级、修复和繁殖共用此底线。
        private float GetFoodReserve(AntColonyState state)
        {
            float daily = 0f;
            foreach (Pawn member in state.Members)
                if (member != null && !member.Dead && !member.Destroyed && member.needs?.food != null)
                    daily += member.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * GenDate.TicksPerDay;
            return daily * Settings.foodReserveDays;
        }

        //函数职责：按巢群局部扫描成熟植物，缓存仅服务所属巢群，避免每只工蚁重复搜索全图。
        private void RefreshHarvestCandidates()
        {
            harvestCandidates.Clear();
            foreach (AntColonyState state in colonies)
            {
                if (state.NestDestroyed || state.Nest == null || !state.Nest.Spawned) continue;
                List<Plant> plants = new List<Plant>();
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(state.NestPosition, Settings.harvestRadius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    Plant plant = cell.GetPlant(map);
                    if (IsHarvestableFood(plant)) plants.Add(plant);
                }
                harvestCandidates.Add(state.Id, plants);
            }
        }

        //仅采收巢周九成成熟的野生食用菌，不割玩家种植作物。
        private bool IsHarvestableFood(Plant plant)
        {
            return plant is Plant_DesertPit && plant.Spawned && plant.Map == map && !plant.sown
                && plant.Growth >= Settings.harvestMinGrowth
                && plant.HarvestableNow && plant.CanYieldNow()
                && plant.def.plant.harvestedThingDef?.IsNutritionGivingIngestible == true
                && !plant.def.plant.harvestedThingDef.IsDrug
                && !map.GetComponent<MapComponent_AntHabitats>().IsRepelled(plant.Position);
        }

        //普通采收需要库位；饥饿的蚁后和工蚁可以直接采菇进食。
        public bool CanHarvestForColony(Pawn pawn, Plant plant, bool forEating = false)
        {
            if (!IsHarvestableFood(plant) || !TryGetColony(pawn, out AntColonyState state) || state.NestDestroyed
                || plant.Position.DistanceToSquared(state.NestPosition) > Settings.harvestRadius * Settings.harvestRadius
                || IsWorkerForageDangerous(pawn, plant.Position)) return false;
            AntCaste caste = pawn.GetComp<Comp_DesertPitAntMember>().Caste;
            if (forEating)
                return (caste == AntCaste.Queen || caste == AntCaste.Worker)
                    && pawn.needs.food.CurLevelPercentage < 0.35f
                    && pawn.RaceProps.CanEverEat(plant.def.plant.harvestedThingDef);
            return caste == AntCaste.Worker && state.HarvestRequested
                && HasHarvestStorageSpace(pawn, state, plant.def.plant.harvestedThingDef);
        }

        //函数职责：检查至少一格可达食物库位，不提前生成物品或随机计算植物产量。
        private bool HasHarvestStorageSpace(Pawn pawn, AntColonyState state, ThingDef product)
        {
            foreach (IntVec3 cell in state.StorageCells)
            {
                if (assignedStorageCells.ContainsKey(cell)) continue;
                Thing occupant = GetStorageOccupant(cell);
                if (occupant != null && (occupant.def != product || occupant.stackCount >= product.stackLimit)) continue;
                if (pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Deadly)) return true;
            }
            return false;
        }

        //选择成熟食用菌，紧急口粮与自行进食不受普通搬运冷却限制。
        private Job TryCreateHarvestJob(Pawn pawn, AntColonyState state, bool forEating = false)
        {
            if (!forEating && GetStoredNutrition(state) >= GetFoodReserve(state)
                && !pawn.GetComp<Comp_DesertPitAntMember>().CanStartForage(Find.TickManager.TicksGame, Settings.workerHaulLimit)) return null;
            List<Plant> candidates;
            if (!harvestCandidates.TryGetValue(state.Id, out candidates)) return null;
            Plant best = null;
            float distance = float.MaxValue;
            foreach (Plant plant in candidates)
            {
                if (!CanHarvestForColony(pawn, plant, forEating)) continue;
                float next = pawn.Position.DistanceToSquared(plant.Position);
                if (next >= distance || !pawn.CanReserveAndReach(plant, PathEndMode.Touch, Danger.Deadly)) continue;
                best = plant;
                distance = next;
            }
            if (best == null) return null;
            Job job = JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_DesertPitAntHarvest, best);
            //目标 B 为直接食用产物的蚂蚁，空目标表示采收入库。
            if (forEating) job.targetB = pawn;
            return job;
        }

        //采收后立即安排食物入库，让采菇指令能补充库存而不是只留下散落产物。
        public void NotifyFungusHarvested(Pawn worker, Thing food, bool forEating)
        {
            RefreshForageCandidates();
            if (forEating)
            {
                if (food != null && worker.CanReserveAndReach(food, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    Job ingest = JobMaker.MakeJob(JobDefOf.Ingest, food);
                    ingest.count = System.Math.Min(food.stackCount, FoodUtility.WillIngestStackCountOf(worker,
                        food.def, FoodUtility.NutritionForEater(worker, food)));
                    worker.jobs.jobQueue.EnqueueFirst(ingest);
                }
                return;
            }
            if (!TryGetColony(worker, out AntColonyState state)) return;
            Job haul = TryCreateForageJob(worker, state, true);
            if (haul != null) worker.jobs.jobQueue.EnqueueFirst(haul);
        }

        //函数职责：储藏断粮时允许成员吃未入库食物，但不取食驱蚁范围内玩家的储备。
        private Thing FindLooseFoodFor(Pawn pawn)
        {
            Thing best = null;
            float distance = float.MaxValue;
            foreach (Thing food in forageCandidates)
            {
                if (!food.Spawned || !IsStoredFood(food) || !food.IngestibleNow || !pawn.RaceProps.CanEverEat(food)
                    || IsWorkerForageDangerous(pawn, food.Position)
                    || map.GetComponent<MapComponent_AntHabitats>().IsRepelled(food.Position)) continue;
                float next = pawn.Position.DistanceToSquared(food.Position);
                if (next >= distance || !pawn.CanReserveAndReach(food, PathEndMode.ClosestTouch, Danger.Deadly)) continue;
                best = food;
                distance = next;
            }
            return best;
        }
    }
}
