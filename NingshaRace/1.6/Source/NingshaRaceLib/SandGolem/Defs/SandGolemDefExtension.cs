using Verse;

namespace NingshaRaceLib.SandGolem.Defs
{
    //类职责：保存沙傀生命周期动画和低频身份维护使用的全局参数。
    public class SandGolemDefExtension : DefModExtension
    {
        //字段职责：定义沙傀汇聚与消散动画持续的游戏 Tick 数。
        public int animationTicks = 120;

        //字段职责：定义沙傀无需求、无关系状态的维护间隔。
        public int maintenanceIntervalTicks = 250;
    }
}
