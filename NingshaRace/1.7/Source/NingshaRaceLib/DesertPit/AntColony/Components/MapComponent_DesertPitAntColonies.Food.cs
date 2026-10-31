using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //统一地面与容器食物的统计和完整扣款。
    public partial class MapComponent_DesertPitAntColonies
    {
        //面板和计划读取缓存；真正扣款必读当前库存。
        public float GetStoredNutrition(AntColonyState state)
        {
            RefreshNutritionIfNeeded(state);
            return state.StoredNutrition;
        }

        //优先以蚁后作为营养换算主体。
        private static Pawn GetNutritionConsumer(AntColonyState state) => state.Queen != null && !state.Queen.Dead
            ? state.Queen : state.Members.Find(member => member != null && !member.Dead && !member.Destroyed);

        //储物穴食物先于蜜露塔，保证普通进食的取食顺序。
        private IEnumerable<Thing> StoredFoods(AntColonyState state)
        {
            foreach (IntVec3 cell in state.StorageCells)
                foreach (Thing thing in cell.GetThingList(map))
                    if (IsStoredFood(thing)) yield return thing;
            foreach (Building_AntColonyStructure building in state.Buildings)
                if (building is Building_AntStoragePit && building.Spawned)
                    foreach (Thing thing in building.Contents)
                        if (IsStoredFood(thing)) yield return thing;
            foreach (Building_AntColonyStructure building in state.Buildings)
                if (building is Building_AntHoneyTower && building.Spawned)
                    foreach (Thing thing in building.Contents)
                        if (IsStoredFood(thing)) yield return thing;
        }

        //每次扣款前重新读取实体数量，绝不根据过期账目扣料。
        private float GetStoredNutrition(AntColonyState state, Pawn eater)
        {
            float total = 0f;
            foreach (Thing food in StoredFoods(state))
                total += FoodUtility.NutritionForEater(eater, food) * (food is Corpse ? 1 : food.stackCount);
            return total;
        }

        //只接收能提供营养的食品与新鲜尸体。
        private static bool IsStoredFood(Thing thing) => thing != null && !thing.Destroyed
            && (thing is Corpse corpse ? corpse.GetRotStage() == RotStage.Fresh
                : thing.def.category == ThingCategory.Item && thing.def.IsNutritionGivingIngestible && !thing.def.IsDrug);

        //先规划全部扣款，尸体和堆叠的取整也不能侵占其他预留和口粮。
        private bool ConsumeStoredNutrition(AntColonyState state, Pawn eater, float required, AntColonyTask task)
        {
            if (eater == null) return false;
            float newMemberReserve = task?.Kind == AntTaskKind.Hatch
                ? Need_Food.BaseHungerRate(task.HatchKind.race.race.lifeStageAges.Last().def, task.HatchKind.race)
                    * GenDate.TicksPerDay * Settings.foodReserveDays : 0f;
            float budget = GetStoredNutrition(state, eater) - GetFoodReserve(state) - ReservedNutrition(state, task) - newMemberReserve;
            if (budget <= required) return false;
            float remaining = required;
            List<KeyValuePair<Thing, int>> payment = new List<KeyValuePair<Thing, int>>();
            foreach (Thing food in StoredFoods(state))
            {
                if (remaining <= 0.0001f) break;
                float nutrition = FoodUtility.NutritionForEater(eater, food);
                if (nutrition <= 0f) continue;
                int count = System.Math.Min(food is Corpse ? 1 : food.stackCount,
                    System.Math.Min(UnityEngine.Mathf.CeilToInt(remaining / nutrition),
                        UnityEngine.Mathf.FloorToInt((budget - 0.0001f) / nutrition)));
                if (count <= 0) continue;
                payment.Add(new KeyValuePair<Thing, int>(food, count));
                budget -= count * nutrition;
                remaining -= count * nutrition;
            }
            if (remaining > 0.0001f) return false;
            foreach (KeyValuePair<Thing, int> entry in payment)
            {
                Thing spent = entry.Key.holdingOwner != null ? entry.Key.holdingOwner.Take(entry.Key, entry.Value)
                    : entry.Key.SplitOff(entry.Value);
                spent.Destroy();
            }
            state.NutritionDirty = true;
            return true;
        }
    }
}
