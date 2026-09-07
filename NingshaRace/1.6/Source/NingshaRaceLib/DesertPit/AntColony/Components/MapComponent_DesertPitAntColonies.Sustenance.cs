using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
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
        private readonly List<Plant> harvestCandidates = new List<Plant>();

        //函数职责：按照现有存活成员实际每日饥饿消耗预留口粮，升级、修复和繁殖共用此底线。
        private float GetFoodReserve(AntColonyState state)
        {
            float daily = 0f;
            foreach (Pawn member in state.Members)
                if (member != null && !member.Dead && !member.Destroyed && member.needs?.food != null)
                    daily += member.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * GenDate.TicksPerDay;
            return daily * Settings.foodReserveDays;
        }

        //函数职责：只允许工蚁采收能产出食物的成熟植物，并在取料全过程服从驱蚁范围。
        public bool CanHarvestForColony(Plant plant)
        {
            return plant != null && plant.Spawned && plant.Map == map && plant.HarvestableNow && plant.CanYieldNow()
                && plant.def.plant.harvestedThingDef?.IsNutritionGivingIngestible == true
                && !plant.def.plant.harvestedThingDef.IsDrug
                && !map.GetComponent<MapComponent_AntHabitats>().IsRepelled(plant.Position);
        }

        //函数职责：为有空闲搬运额度的工蚁选择最近可达的成熟食用菌，不搬走整株植物。
        private Job TryCreateHarvestJob(Pawn pawn, AntColonyState state)
        {
            if (!pawn.TryGetComp<Comp_DesertPitAntMember>().CanStartForage(Find.TickManager.TicksGame, Settings.workerHaulLimit)) return null;
            Plant best = null;
            float distance = float.MaxValue;
            foreach (Plant plant in harvestCandidates)
            {
                if (!CanHarvestForColony(plant)) continue;
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
