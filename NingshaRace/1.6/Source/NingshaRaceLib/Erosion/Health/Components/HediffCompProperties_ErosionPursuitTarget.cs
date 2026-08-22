using System;
using Verse;

namespace NingshaRaceLib.Erosion.Health.Components
{
    //类职责：把侵蚀体 Hediff 配置绑定到追杀目标数据组件。
    public sealed class HediffCompProperties_ErosionPursuitTarget : HediffCompProperties
    {
        //构造函数职责：指定追杀目标组件的运行时类型。
        public HediffCompProperties_ErosionPursuitTarget()
        {
            compClass = typeof(HediffComp_ErosionPursuitTarget);
        }
    }
}
