using NingshaRaceLib.DesertPit.AntColony.State;
using NingshaRaceLib.DesertPit.Ecology.Habitats;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //营养账目每天完整统计，库存与成员事件提前标记更新。
    public partial class MapComponent_DesertPitAntColonies
    {
        //即使成员暂时入巢，也按全巢需求预留一天口粮。
        private float GetFoodReserve(AntColonyState state)
        {
            RefreshNutritionIfNeeded(state);
            return state.DailyNutrition * Settings.foodReserveDays;
        }

        //按实际存活成员计算需求，蚁后单独加入且不重复计数。
        private void RefreshNutritionIfNeeded(AntColonyState state)
        {
            int now = Find.TickManager.TicksGame;
            if (!state.NutritionDirty && now < state.NextNutritionTick) return;
            state.DailyNutrition = DailyFood(state.Queen);
            foreach (Pawn member in state.Members) state.DailyNutrition += DailyFood(member);
            Pawn eater = GetNutritionConsumer(state);
            state.StoredNutrition = eater == null ? 0f : GetStoredNutrition(state, eater);
            state.NutritionDirty = false;
            state.NextNutritionTick = now + GenDate.TicksPerDay;
        }

        //使用原版实际饥饿速度换算一天的消耗。
        private static float DailyFood(Pawn pawn) => pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.needs?.food != null
            ? pawn.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * GenDate.TicksPerDay : 0f;

        //物品入库、取食、生产与消耗共用账目更新入口。
        public void NotifyInventoryChanged(int colonyId)
        {
            if (coloniesById.TryGetValue(colonyId, out AntColonyState state)) state.NutritionDirty = true;
        }

        //没有储备食物时才使用地图缓存中的散落食物。
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
