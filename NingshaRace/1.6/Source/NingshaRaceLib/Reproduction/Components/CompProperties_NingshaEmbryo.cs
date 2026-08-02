using Verse;

namespace NingshaRaceLib.Reproduction.Components
{
    //类职责：提供受精凝砂卵孵化天数与安全温度范围的 XML 配置。
    public class CompProperties_NingshaEmbryo : CompProperties
    {
        //字段职责：记录受精卵从零进度到破壳所需的游戏天数。
        public float hatchDays = 12f;

        //字段职责：记录允许孵化推进的最低环境温度。
        public float minimumTemperature;

        //字段职责：记录允许孵化推进的最高环境温度。
        public float maximumTemperature = 50f;

        //构造函数职责：把属性实例绑定到受精凝砂卵组件。
        public CompProperties_NingshaEmbryo()
        {
            compClass = typeof(CompNingshaEmbryo);
        }
    }
}
