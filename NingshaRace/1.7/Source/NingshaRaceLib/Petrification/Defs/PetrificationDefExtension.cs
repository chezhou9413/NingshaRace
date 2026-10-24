using Verse;

namespace NingshaRaceLib.Petrification.Defs
{
    //类职责：保存石化状态的满层持续时间与未继续累积时的消退时间。
    public class PetrificationDefExtension : DefModExtension
    {
        //字段职责：定义石化进度停止增长后自动移除所需的游戏 Tick 数。
        public int inactivityDurationTicks = 30000;

        //字段职责：定义完全石化状态持续的游戏 Tick 数。
        public int fullPetrificationDurationTicks = 2500;
    }
}
