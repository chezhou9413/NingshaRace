using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.State
{
    //类职责：持久化一只常规蚂蚁的死亡时间、地点及撤退和调查消费状态。
    public sealed class AntDeathRecord : IExposable
    {
        //字段职责：记录死亡发生的游戏 Tick。
        public int Tick;

        //字段职责：记录死亡发生的地图格。
        public IntVec3 Position;

        //字段职责：标记这次死亡是否已经参与触发撤退，避免同一批伤亡重复触发。
        public bool CountedForRetreat;

        //字段职责：标记这次死亡是否已经用于派遣调查队。
        public bool CountedForInvestigation;

        //函数职责：供存档系统通过无参构造函数建立死亡记录。
        public AntDeathRecord()
        {
        }

        //函数职责：用死亡时间与位置建立尚未消费的伤亡记录。
        public AntDeathRecord(int tick, IntVec3 position)
        {
            Tick = tick;
            Position = position;
        }

        //函数职责：保存死亡时间、位置和两种行为触发的消费标记。
        public void ExposeData()
        {
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref Position, "position");
            Scribe_Values.Look(ref CountedForRetreat, "countedForRetreat", false);
            Scribe_Values.Look(ref CountedForInvestigation, "countedForInvestigation", false);
        }
    }
}
