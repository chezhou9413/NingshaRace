using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Habitats
{
    //类职责：配置驱蚁桩的作用距离和独立失效周期。
    public sealed class DefModExtension_AntRepellent : DefModExtension
    {
        public float radius = 12f;
        public int cycleTicks = 15000;
        public float failureChance = 0.1f;

        //函数职责：在定义加载时报告不合法的范围、时长和概率。
        public override IEnumerable<string> ConfigErrors()
        {
            if (radius <= 0f || cycleTicks <= 0 || failureChance < 0f || failureChance > 1f)
                yield return "驱蚁桩的范围和周期必须为正数，失效概率必须介于零和一之间。";
        }
    }
}
