using System;
using RimWorld;

namespace NingshaRaceLib.Storyteller.Quests.Scheduling
{
    //类职责：声明索提斯保证触发侵蚀追杀任务时使用的固定事件。
    public sealed class StorytellerCompProperties_NingshaErosionPursuit : StorytellerCompProperties
    {
        //字段职责：索提斯到达保证触发时间后尝试执行的任务事件。
        public IncidentDef incident;

        //构造函数职责：把属性定义绑定到侵蚀追杀任务的叙事者组件。
        public StorytellerCompProperties_NingshaErosionPursuit()
        {
            compClass = typeof(StorytellerComp_NingshaErosionPursuit);
        }
    }
}
