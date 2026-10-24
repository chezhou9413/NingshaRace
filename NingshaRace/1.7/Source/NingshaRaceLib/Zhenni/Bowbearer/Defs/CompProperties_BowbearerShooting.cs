using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//将真实射击状态与独立美术定义绑定到单位。
public sealed class CompProperties_BowbearerShooting : CompProperties
{
    public BowbearerShootingDef shooting;

    //指定挽弓者射击组件。
    public CompProperties_BowbearerShooting() { compClass = typeof(CompBowbearerShooting); }

    //射击配置必须明确存在。
    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (shooting == null) yield return "挽弓者缺少 shooting 定义。";
    }
}
