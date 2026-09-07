using System.Collections.Generic;
using NingshaRaceLib.Loot.Config;
using Verse;

namespace NingshaRaceLib.Loot.Components
{
    //类职责：在原版死亡遗留物流程中结算一项奖励，记录身份级别的结算状态。
    public sealed class CompDeathLoot : ThingComp
    {
        private bool settled;

        //属性职责：读取物品表与异变匹配条件。
        private CompProperties_DeathLoot Settings => (CompProperties_DeathLoot)props;

        //函数职责：保存结算标记，防止复活、读档或重复死亡通知再次产出奖励。
        public override void PostExposeData() => Scribe_Values.Look(ref settled, "deathLootSettled");

        //函数职责：仅在真实死亡结算时返回一种奖励，由原版负责在死亡位置放置物品。
        public override IEnumerable<ThingDefCountClass> GetAdditionalLeavings(Map map, DestroyMode mode)
        {
            Pawn pawn = parent as Pawn;
            if (settled || mode != DestroyMode.KillFinalize || pawn == null || !pawn.Dead) yield break;
            if (Settings.requiredMutant != null && (!pawn.IsMutant || pawn.mutant.Def != Settings.requiredMutant)) yield break;
            settled = true;
            //原版遗留物流程已经用生物编号隔离随机状态，不再重新设定随机种子。
            int roll = Rand.Range(0, 100);
            foreach (DeathLootEntry entry in Settings.entries)
            {
                roll -= entry.weight;
                if (roll >= 0) continue;
                yield return new ThingDefCountClass(entry.thingDef, entry.count.RandomInRange);
                yield break;
            }
        }
    }
}
