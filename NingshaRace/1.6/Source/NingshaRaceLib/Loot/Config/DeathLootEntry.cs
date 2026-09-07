using Verse;

namespace NingshaRaceLib.Loot.Config
{
    //类职责：保存一项互斥死亡奖励的物品、整数权重及数量范围。
    public sealed class DeathLootEntry
    {
        public ThingDef thingDef;
        public int weight;
        public IntRange count;
    }
}
