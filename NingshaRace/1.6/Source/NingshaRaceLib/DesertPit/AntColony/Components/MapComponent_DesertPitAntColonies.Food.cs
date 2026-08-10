using RimWorld;
using Verse;

using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：集中实现蚁群实体储备的营养统计、食物识别和繁殖消耗。
    public partial class MapComponent_DesertPitAntColonies
    {
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

        //函数职责：从实体储藏格中消耗指定营养，并优先扣除补员任务实际展示的食物。
        private bool ConsumeStoredNutrition(AntColonyState state, Pawn eater, float requiredNutrition, Thing preferredFood)
        {
            if (GetStoredNutrition(state, eater) < requiredNutrition)
            {
                return false;
            }

            float remaining = requiredNutrition;
            if (IsFoodStoredInColony(state, preferredFood))
            {
                ConsumeNutritionFromFood(preferredFood, eater, ref remaining);
            }

            for (int i = 0; i < state.StorageCells.Count && remaining > 0f; i++)
            {
                Thing food = GetStorageOccupant(state.StorageCells[i]);
                if (food == preferredFood || !IsStoredFood(food))
                {
                    continue;
                }

                ConsumeNutritionFromFood(food, eater, ref remaining);
            }

            return remaining <= 0f;
        }

        //函数职责：确认指定食物仍是当前巢群储藏格中的有效实体，避免结算已经被搬走的物资。
        private bool IsFoodStoredInColony(AntColonyState state, Thing food)
        {
            return IsStoredFood(food) && state.StorageCells.Contains(food.Position) && GetStorageOccupant(food.Position) == food;
        }

        //函数职责：从单个食物实体扣除剩余需求，尸体整具消耗，普通堆叠按实际营养数量拆分。
        private static void ConsumeNutritionFromFood(Thing food, Pawn eater, ref float remaining)
        {
            float nutrition = FoodUtility.NutritionForEater(eater, food);
            if (food is Corpse)
            {
                remaining -= nutrition;
                food.Destroy();
                return;
            }

            int consumeCount = System.Math.Min(food.stackCount, UnityEngine.Mathf.CeilToInt(remaining / UnityEngine.Mathf.Max(nutrition, 0.001f)));
            remaining -= nutrition * consumeCount;
            if (consumeCount >= food.stackCount)
            {
                food.Destroy();
                return;
            }

            food.SplitOff(consumeCount).Destroy();
        }
    }
}
