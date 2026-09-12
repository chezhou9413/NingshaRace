using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：维护食用菌采收缓存和巢群口粮底线，连接自然生长、采收、搬运与进食。
    public partial class MapComponent_DesertPitAntColonies
    {
        private readonly Dictionary<int, List<Plant>> harvestCandidates = new Dictionary<int, List<Plant>>();

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

        //函数职责：识别达到指定成熟度且原版允许采收的健康食用植物。
        private bool IsHarvestableFood(Plant plant)
        {
            return plant != null && plant.Spawned && plant.Map == map && plant.Growth >= Settings.harvestMinGrowth
                && plant.HarvestableNow && plant.CanYieldNow()
                && plant.def.plant.harvestedThingDef?.IsNutritionGivingIngestible == true
                && !plant.def.plant.harvestedThingDef.IsDrug
                && !map.GetComponent<MapComponent_AntHabitats>().IsRepelled(plant.Position);
        }

        //函数职责：为任务选择、持续执行和最终结算统一验证所属巢群、范围、驱蚁影响及储藏空间。
        public bool CanHarvestForColony(Pawn pawn, Plant plant)
        {
            AntColonyState state;
            return IsHarvestableFood(plant) && TryGetColony(pawn, out state) && !state.NestDestroyed
                && pawn.TryGetComp<Comp_DesertPitAntMember>()?.Caste == AntCaste.Worker
                && plant.Position.DistanceToSquared(state.NestPosition) <= Settings.harvestRadius * Settings.harvestRadius
                && !IsWorkerForageDangerous(pawn, plant.Position)
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

        //函数职责：为有空闲搬运额度的工蚁选择最近可达的成熟食用菌，不搬走整株植物。
        private Job TryCreateHarvestJob(Pawn pawn, AntColonyState state)
        {
            if (!pawn.TryGetComp<Comp_DesertPitAntMember>().CanStartForage(Find.TickManager.TicksGame, Settings.workerHaulLimit)) return null;
            List<Plant> candidates;
            if (!harvestCandidates.TryGetValue(state.Id, out candidates)) return null;
            Plant best = null;
            float distance = float.MaxValue;
            foreach (Plant plant in candidates)
            {
                if (!CanHarvestForColony(pawn, plant)) continue;
                float next = pawn.Position.DistanceToSquared(plant.Position);
                if (next >= distance || !pawn.CanReserveAndReach(plant, PathEndMode.Touch, Danger.Deadly)) continue;
                best = plant;
                distance = next;
            }
            return best == null ? null : JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_DesertPitAntHarvest, best);
        }

        //函数职责：采收产生真实物品后立即更新物资缓存，使下一步能够搬运或进食。
        public void NotifyFungusHarvested() => RefreshForageCandidates();

        //函数职责：储藏断粮时允许成员吃未入库食物，但不取食驱蚁范围内玩家的储备。
        private Thing FindLooseFoodFor(Pawn pawn)
        {
            Thing best = null;
            float distance = float.MaxValue;
            foreach (Thing food in forageCandidates)
            {
                if (!food.Spawned || !IsStoredFood(food) || !food.IngestibleNow
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
