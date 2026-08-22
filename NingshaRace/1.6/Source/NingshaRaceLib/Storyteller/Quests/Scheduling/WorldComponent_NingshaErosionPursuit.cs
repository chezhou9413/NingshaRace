using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Storyteller.Quests.Scheduling
{
    //类职责：为当前存档保存侵蚀追杀任务的一次性触发状态与索提斯保证触发时间。
    public sealed class WorldComponent_NingshaErosionPursuit : WorldComponent
    {
        private const int TicksPerDay = 60000;
        private const int FirstPossibleTick = TicksPerDay;
        private const int GuaranteedDeadlineTick = TicksPerDay * 3;

        private bool offerConsumed;
        private int sotisiGuaranteedOfferTick = -1;

        //属性职责：指示当前存档是否已经出现过侵蚀追杀任务信。
        public bool OfferConsumed => offerConsumed;

        //属性职责：指示索提斯的保证触发时间是否已经到达。
        public bool SotisiGuaranteedOfferDue => sotisiGuaranteedOfferTick >= 0
            && Find.TickManager.TicksGame >= sotisiGuaranteedOfferTick;

        //构造函数职责：把一次性任务状态附加到当前世界并参与存档序列化。
        public WorldComponent_NingshaErosionPursuit(World world) : base(world)
        {
        }

        //函数职责：在索提斯首次检查任务时确定第一个至第三个游戏日之间的持久化触发点。
        public void EnsureSotisiSchedule()
        {
            if (sotisiGuaranteedOfferTick >= 0)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick >= GuaranteedDeadlineTick)
            {
                sotisiGuaranteedOfferTick = currentTick;
                return;
            }

            int lowerBound = Mathf.Clamp(currentTick, FirstPossibleTick, GuaranteedDeadlineTick);
            sotisiGuaranteedOfferTick = Rand.RangeInclusive(lowerBound, GuaranteedDeadlineTick);
        }

        //函数职责：在任务信成功创建后永久消耗当前存档的唯一触发机会。
        public void MarkOfferConsumed()
        {
            offerConsumed = true;
        }

        //函数职责：保存并读取一次性标记和索提斯保证触发时间。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref offerConsumed, "offerConsumed", false);
            Scribe_Values.Look(ref sotisiGuaranteedOfferTick, "sotisiGuaranteedOfferTick", -1);
        }
    }
}
