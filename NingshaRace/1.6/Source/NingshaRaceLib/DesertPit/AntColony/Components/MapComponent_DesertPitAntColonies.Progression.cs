using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：集中处理可升级蚁巢的营养消耗、等级目标刷新与检查面板文本。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：取得指定存活蚁穴对应的巢群状态。
        public bool TryGetColony(Building_DesertPitAntNest nest, out AntColonyState state)
        {
            if (nest == null)
            {
                state = null;
                return false;
            }

            return coloniesByNest.TryGetValue(nest, out state);
        }

        //函数职责：在周期更新中验证最高等级、蚁后、冷却和营养并自动完成一次升级。
        private void TryUpgradeColony(AntColonyState state, int ticks)
        {
            if (!state.LevelingEnabled || state.CurrentLevel >= state.MaxLevel || ticks < state.NextUpgradeTick)
            {
                return;
            }

            if (state.NestDestroyed || state.Nest == null || state.Nest.Destroyed || state.Queen == null || state.Queen.Dead || state.Queen.Destroyed)
            {
                return;
            }

            float requiredNutrition = Settings.GetUpgradeNutrition(state.CurrentLevel);
            if (requiredNutrition <= 0f || GetStoredNutrition(state, state.Queen) < requiredNutrition)
            {
                return;
            }

            if (!ConsumeStoredNutrition(state, state.Queen, requiredNutrition, null))
            {
                return;
            }

            IncreaseColonyLevel(state, ticks);
        }

        //函数职责：让上帝模式无视营养、蚁后和冷却强制提升一级，但不允许超过本巢最高等级。
        public bool DebugForceUpgrade(AntColonyState state)
        {
            if (state == null || !state.LevelingEnabled || state.CurrentLevel >= state.MaxLevel || state.NestDestroyed || state.Nest == null || state.Nest.Destroyed)
            {
                return false;
            }

            IncreaseColonyLevel(state, Find.TickManager.TicksGame);
            return true;
        }

        //函数职责：统一提升巢群等级、刷新工兵目标并写入正常七天升级冷却。
        private void IncreaseColonyLevel(AntColonyState state, int ticks)
        {
            state.CurrentLevel++;
            state.Population = AntColonyPopulationSettings.CreateForLevel(Settings, state.CurrentLevel);
            state.NextUpgradeTick = ticks + Settings.upgradeCooldownTicks;
        }

        //函数职责：生成蚁穴检查面板中的等级、营养、门槛和升级冷却信息。
        public string GetColonyInspectString(AntColonyState state)
        {
            if (state == null)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            if (state.LevelingEnabled)
            {
                builder.Append("蚁巢等级：").Append(state.CurrentLevel).Append(" / ").Append(state.MaxLevel);
                builder.AppendLine();
                builder.Append("储藏营养：").Append(GetStoredNutrition(state).ToString("0.##"));
                if (state.CurrentLevel < state.MaxLevel)
                {
                    builder.AppendLine();
                    builder.Append("下一级门槛：").Append(Settings.GetUpgradeNutrition(state.CurrentLevel).ToString("0.##"));
                    int remaining = Mathf.Max(0, state.NextUpgradeTick - Find.TickManager.TicksGame);
                    if (remaining > 0)
                    {
                        builder.AppendLine();
                        builder.Append("升级冷却：").Append(remaining.ToStringTicksToPeriod());
                    }
                }
                else
                {
                    builder.AppendLine();
                    builder.Append("已达到该蚁巢的最高等级");
                }
            }
            else
            {
                builder.Append("固定规模蚁巢");
            }

            if (IsRetreating(state, Find.TickManager.TicksGame))
            {
                builder.AppendLine();
                builder.Append("撤退剩余：").Append((state.RetreatUntilTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());
            }

            return builder.ToString();
        }
    }
}
