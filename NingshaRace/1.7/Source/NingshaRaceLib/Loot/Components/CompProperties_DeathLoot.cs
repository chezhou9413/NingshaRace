using System.Collections.Generic;
using NingshaRaceLib.Loot.Config;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Loot.Components
{
    //类职责：配置死亡奖励表和可选异变限制，校验权重及产物数量。
    public sealed class CompProperties_DeathLoot : CompProperties
    {
        public MutantDef requiredMutant;
        public List<DeathLootEntry> entries = new List<DeathLootEntry>();

        //函数职责：把奖励配置绑定到仅参与死亡遗留物结算的组件。
        public CompProperties_DeathLoot() : base(typeof(CompDeathLoot)) { }

        //函数职责：拒绝缺失产物、非法数量和不等于百分之百的奖励表。
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            int total = 0;
            foreach (DeathLootEntry entry in entries)
            {
                total += entry.weight;
                if (entry.thingDef == null || entry.weight <= 0 || entry.count.min <= 0 || entry.count.max < entry.count.min)
                    yield return "死亡奖励必须指定物品、正数权重和有效的正整数数量区间。";
            }
            if (total != 100) yield return "死亡奖励的互斥权重总和必须为一百。";
            if (parentDef.race == null) yield return "死亡奖励组件只能配置到生物。";
        }
    }
}
