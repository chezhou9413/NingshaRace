using System.Collections.Generic;
using Verse;

using NingshaRaceLib.Combat.GroundSpike.Rendering;
using NingshaRaceLib.Combat.GroundSpike.Utility;
using NingshaRaceLib.Combat.GroundSpike.Verbs;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.GroundSpike.Tracking
{
    //类职责：分别保存一排地刺的唯一视觉格、横向伤害格和动画结算时间。
    public class GroundSpikeWaveStep
    {
        //字段职责：保存当前横排唯一地刺 Mote 的中心生成格。
        public readonly IntVec3 visualCell;

        //字段职责：保存当前横排实际覆盖且不与前排重复的伤害格。
        public readonly List<IntVec3> damageCells;

        //字段职责：记录当前横排生成中心 Mote 的游戏 Tick。
        public readonly int spawnTick;

        //字段职责：记录当前横排进入伤害帧的游戏 Tick。
        public readonly int impactTick;

        //字段职责：记录当前横排是否已经生成唯一视觉效果。
        public bool spawned;

        //字段职责：记录当前横排是否已经完成三格伤害结算。
        public bool impacted;

        //构造函数职责：建立一排地刺的视觉与伤害推进状态。
        public GroundSpikeWaveStep(List<IntVec3> damageCells, IntVec3 visualCell, int spawnTick, int impactTick)
        {
            this.damageCells = damageCells;
            this.visualCell = visualCell;
            this.spawnTick = spawnTick;
            this.impactTick = impactTick;
        }
    }
}
