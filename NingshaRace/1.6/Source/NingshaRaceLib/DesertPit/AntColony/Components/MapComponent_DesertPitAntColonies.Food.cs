using RimWorld;
using System.Collections.Generic;
using Verse;

using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：集中实现蚁群实体储备的营养统计、食物识别和繁殖消耗。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：为检查面板和升级逻辑计算指定巢群当前可消费的实体储藏营养。
        public float GetStoredNutrition(AntColonyState state)
        {
            Pawn eater = GetNutritionConsumer(state);
            return eater == null ? 0f : GetStoredNutrition(state, eater);
        }

        //函数职责：优先选择蚁后并在蚁后失效时选择任一存活成员，作为营养换算与消费主体。
        private static Pawn GetNutritionConsumer(AntColonyState state)
        {
            Pawn eater = state?.Queen;
            if (eater == null || eater.Dead || eater.Destroyed)
            {
                eater = state?.Members.Find(member => member != null && !member.Dead && !member.Destroyed);
            }

            return eater;
        }

        //函数职责：计算指定巢群实体储藏格内可用于进食和繁殖的总营养。
        private float GetStoredNutrition(AntColonyState state, Pawn eater)
        {
            float total = 0f;
            for (int i = 0; i < state.StorageCells.Count; i++)
            {
                Thing food = GetStorageOccupant(state.StorageCells[i]);
                if (!IsStoredFood(food))
                {
                    continue;
                }

                float nutrition = FoodUtility.NutritionForEater(eater, food);
                total += food is Corpse ? nutrition : nutrition * food.stackCount;
            }

            return total;
        }

        //函数职责：判断储藏实体是否属于允许蚁群消耗的新鲜食物。
        private static bool IsStoredFood(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            Corpse corpse = thing as Corpse;
            if (corpse != null)
            {
                return corpse.GetRotStage() == RotStage.Fresh;
            }

            return thing.def.IsNutritionGivingIngestible && !thing.def.IsDrug;
        }

        //函数职责：先规划完整扣款再消耗物资，整具尸体和堆叠向上取整都不能侵占保留口粮。
        private bool ConsumeStoredNutrition(AntColonyState state, Pawn eater, float requiredNutrition, Thing preferredFood)
        {
            float budget = GetStoredNutrition(state, eater) - GetFoodReserve(state);
            if (budget < requiredNutrition) return false;
            float remaining = requiredNutrition;
            List<KeyValuePair<Thing, int>> payment = new List<KeyValuePair<Thing, int>>();
            if (IsFoodStoredInColony(state, preferredFood))
                PlanFoodPayment(preferredFood, eater, payment, ref remaining, ref budget);
            for (int i = 0; i < state.StorageCells.Count && remaining > 0f; i++)
            {
                Thing food = GetStorageOccupant(state.StorageCells[i]);
                if (food == preferredFood || !IsStoredFood(food)) continue;
                PlanFoodPayment(food, eater, payment, ref remaining, ref budget);
            }
            if (remaining > 0.0001f) return false;
            foreach (KeyValuePair<Thing, int> entry in payment)
                if (entry.Value == entry.Key.stackCount) entry.Key.Destroy();
                else entry.Key.SplitOff(entry.Value).Destroy();
            return true;
        }

        //函数职责：确认指定食物仍是当前巢群储藏格中的有效实体，避免结算已经被搬走的物资。
        private bool IsFoodStoredInColony(AntColonyState state, Thing food)
        {
            return IsStoredFood(food) && food.Spawned && food.Map == map && state.StorageCells.Contains(food.Position) && GetStorageOccupant(food.Position) == food;
        }

        //函数职责：为单个物资计算可支付单位数，过大的尸体暂不消耗，未凑齐完整金额时不破坏任何物件。
        private static void PlanFoodPayment(Thing food, Pawn eater, List<KeyValuePair<Thing, int>> payment, ref float remaining, ref float budget)
        {
            float nutrition = FoodUtility.NutritionForEater(eater, food);
            if (nutrition <= 0f || remaining <= 0f) return;
            int needed = UnityEngine.Mathf.CeilToInt(remaining / nutrition);
            int affordable = UnityEngine.Mathf.FloorToInt(budget / nutrition);
            int count = System.Math.Min(food is Corpse ? 1 : food.stackCount, System.Math.Min(needed, affordable));
            if (count <= 0) return;
            payment.Add(new KeyValuePair<Thing, int>(food, count));
            remaining -= nutrition * count;
            budget -= nutrition * count;
        }
    }
}
