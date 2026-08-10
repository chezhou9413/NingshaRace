using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Combat.GroundSpike.Tracking
{
    //类职责：保存砂岩棘环单层圆环的覆盖格、生成时间和伤害结算时间。
    public sealed class GroundSpikeRingStep
    {
        //字段职责：保存当前圆环内全部有效地图格。
        public readonly List<IntVec3> cells;

        //字段职责：记录当前圆环生成地刺 Mote 的游戏 Tick。
        public readonly int spawnTick;

        //字段职责：记录当前圆环进入伤害帧的游戏 Tick。
        public readonly int impactTick;

        //字段职责：记录当前圆环是否已经生成视觉效果。
        public bool spawned;

        //字段职责：记录当前圆环是否已经完成伤害结算。
        public bool impacted;

        //构造函数职责：建立一层圆环的地图格与时间状态。
        public GroundSpikeRingStep(List<IntVec3> cells, int spawnTick, int impactTick)
        {
            this.cells = cells;
            this.spawnTick = spawnTick;
            this.impactTick = impactTick;
        }
    }
}
