using UnityEngine;
using Verse;

using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：集中处理全部自定义蚁穴按实体储藏营养执行的周期自动修复。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：在受击等待和修复间隔结束后按实际缺失耐久折算营养并恢复蚁穴。
        private void TryRepairNest(AntColonyState state, int ticks)
        {
            Building_DesertPitAntNest nest = state?.Nest;
            if (state == null || state.NestDestroyed || nest == null || nest.Destroyed || !nest.Spawned || nest.HitPoints >= nest.MaxHitPoints)
            {
                return;
            }

            if (ticks < state.NextRepairTick)
            {
                return;
            }

            int damageDelayEnd = state.LastNestDamageTick < 0 ? 0 : state.LastNestDamageTick + Settings.repairDelayAfterDamageTicks;
            if (ticks < damageDelayEnd)
            {
                state.NextRepairTick = damageDelayEnd;
                return;
            }

            Pawn eater = GetNutritionConsumer(state);
            if (eater == null)
            {
                return;
            }

            int repairAmount = Mathf.Min(Settings.repairHitPoints, nest.MaxHitPoints - nest.HitPoints);
            float nutritionCost = Settings.repairNutritionCost * repairAmount / Settings.repairHitPoints;
            if (GetStoredNutrition(state, eater) < nutritionCost || !ConsumeStoredNutrition(state, eater, nutritionCost, null))
            {
                return;
            }

            nest.HitPoints = Mathf.Min(nest.MaxHitPoints, nest.HitPoints + repairAmount);
            state.NextRepairTick = ticks + Settings.repairIntervalTicks;
        }
    }
}
