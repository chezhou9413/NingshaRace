using Verse;
using RimWorld;

namespace NingshaRaceLib.DesertPit.Ecology.Habitats
{
    //类职责：以不可通过搬动或读档重抽的独立周期阻止工蚁收集范围内物资。
    public sealed class Building_AntRepellent : Building_AntHabitat
    {
        private int epochTick = -1;
        private int cachedCycle = -1;
        private bool running;
        private DefModExtension_AntRepellent Settings => def.GetModExtension<DefModExtension_AntRepellent>();
        public override float Radius => Settings.radius;
        protected override bool EmitsSpores => Running;

        //属性职责：用物件编号和绝对周期决定整周期状态，不消耗游戏全局随机序列。
        public bool Running
        {
            get
            {
                int cycle = (Find.TickManager.TicksGame - epochTick) / Settings.cycleTicks;
                if (cycle != cachedCycle)
                {
                    cachedCycle = cycle;
                    running = !Rand.ChanceSeeded(Settings.failureChance, Gen.HashCombineInt(thingIDNumber, cycle));
                }
                return running;
            }
        }

        //函数职责：首次生成时记录周期起点，搬动和重新安装不重置起点。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            if (epochTick < 0) epochTick = Find.TickManager.TicksGame;
            base.SpawnSetup(map, respawningAfterLoad);
        }

        //函数职责：保存原始周期起点，读档后按同一物件编号恢复相同的周期状态。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref epochTick, "repellentEpoch", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) cachedCycle = -1;
        }

        //函数职责：使用自然语言说明作用状态、下一次变化时间和非免战区限制。
        public override string GetInspectString()
        {
            int remaining = Settings.cycleTicks - (Find.TickManager.TicksGame - epochTick) % Settings.cycleTicks;
            return InspectPrefix() + (Running ? "气味浓郁：附近物资不会被工蚁收集" : "气味散去：暂时无法保护物资")
                + "\n下次变化：" + remaining.ToStringTicksToPeriod() + "\n作用范围：" + Radius.ToString("0.#") + "格；不阻止蚂蚁通行或攻击";
        }
    }
}
