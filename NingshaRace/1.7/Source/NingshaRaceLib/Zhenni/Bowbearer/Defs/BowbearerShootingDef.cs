using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//射击动画和箭矢表现共用的可交付定义。
public sealed class BowbearerShootingDef : Def
{
    public BowbearerSettings settings = new();
    private BowbearerSettings empoweredVisuals;
    internal BowbearerSettings EmpoweredVisuals => empoweredVisuals ??= settings.empowered.Visuals(settings);
    internal ThingDef EmpoweredProjectile => DefDatabase<ThingDef>.GetNamed("NingshaRace_Zhenni_Bowbearer_EmpoweredArrow");

    //加载时直接报告非法帧序和特效参数。
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        foreach (string error in settings.Errors()) yield return error;
    }
}

