using System.Collections.Generic;
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

        //字段职责：定义无法继承召唤者技能时使用的基础技能等级。
        public int fallbackSkillLevel = 6;

        //函数职责：在 Def 加载时校验沙傀生命周期和技能参数处于有效范围。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (animationTicks <= 0)
            {
                yield return "沙傀 animationTicks 必须大于零。";
            }
            if (maintenanceIntervalTicks <= 0)
            {
                yield return "沙傀 maintenanceIntervalTicks 必须大于零。";
            }
            if (fallbackSkillLevel < 0 || fallbackSkillLevel > 20)
            {
                yield return "沙傀 fallbackSkillLevel 必须位于 0 到 20 之间。";
            }
        }
    }
}
