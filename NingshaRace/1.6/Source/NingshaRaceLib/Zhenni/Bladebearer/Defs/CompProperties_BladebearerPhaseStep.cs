using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //拥刀者的追敌瞬移与消散时长，不增加全局能力框架。
    public sealed class CompProperties_BladebearerPhaseStep : CompProperties
    {
        public int cooldownTicks = 300, arrivalTicks = 30;
        public float minRange = 3.5f, maxRange = 12f, departureSeconds = 0.48f, deathSeconds = 1.25f;
        //由 Pawn 组件持有冷却与效果状态。
        public CompProperties_BladebearerPhaseStep() { compClass = typeof(CompBladebearerPhaseStep); }
        //阻止无效范围或时长进入自动释放逻辑。
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (cooldownTicks <= 0 || arrivalTicks <= 0 || departureSeconds <= 0 || deathSeconds <= 0)
                yield return "瞬移冷却、聚合和消散时长必须大于零。";
            if (minRange <= 1.5f || maxRange <= minRange) yield return "瞬移最大距离必须大于最小距离，最小距离须超过近战距离。";
        }
    }
}
