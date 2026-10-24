using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//强化箭的视觉差异独立保存，伤害和爆炸范围仍由弹丸 Def 决定。
public sealed class BowbearerEmpoweredSettings
{
    public float warmupMultiplier = 1.75f;
    public float flashLead = .65f, flashDuration = .36f, flashSize = 2.1f, flashIntensity = 2f;
    public float arrowScale = 1.25f;
    public Color arrowColor = new(.72f, .85f, 1, 1);
    public float radius = .28f, pitch = .75f, width = .075f, length = 4.5f, lifetime = .38f;
    public float rotationsPerSecond = 2.2f, trailIntensity = .8f, trailGlow = .3f;
    public Color trailColor = new(.35f, .65f, 1, 1);
    public float impactRadius = 2.1f, impactDuration = .95f, impactIntensity = 1f, impactGlow = .3f, impactDistortion = .85f;
    public int impactAshCount = 48;
    public Color impactColor = new(.55f, .28f, 1, 1);

    //值类型字段直接复制，草稿与默认参数互不影响。
    internal BowbearerEmpoweredSettings Copy() => (BowbearerEmpoweredSettings)MemberwiseClone();

    //沿用普通箭的图层和衔接点，在独立副本上应用强化配色与尺寸。
    internal BowbearerSettings Visuals(BowbearerSettings source)
    {
        BowbearerSettings result = source.Copy();
        result.Empowered = true;
        foreach (var part in new[] { result.arrowHead, result.arrowTail })
        { part.size *= arrowScale; part.position *= arrowScale; part.color *= arrowColor; }
        result.radius = radius; result.pitch = pitch; result.width = width; result.length = length;
        result.lifetime = lifetime; result.rotationsPerSecond = rotationsPerSecond;
        result.trailIntensity = trailIntensity; result.trailGlow = trailGlow; result.trailColor = trailColor;
        result.impactRadius = impactRadius; result.impactDuration = impactDuration; result.impactIntensity = impactIntensity;
        result.impactGlow = impactGlow; result.impactDistortion = impactDistortion;
        result.impactAshCount = impactAshCount; result.impactColor = impactColor;
        return result;
    }

    //强化页的输入单独校验，拒绝非有限数值与超出控件范围的预设。
    internal IEnumerable<string> Errors()
    {
        foreach (FieldInfo field in GetType().GetFields())
            if (field.GetValue(this) is float value && !float.IsFinite(value)) yield return "强化箭「" + field.Name + "」必须为有限数值。";
        if (!Range(warmupMultiplier, 1.1f, 4)) yield return "强化蓄力倍率必须为 1.1～4。";
        if (!Range(flashLead, .1f, 2) || !Range(flashDuration, .05f, 1) || flashDuration > flashLead
            || !Range(flashSize, .2f, 4) || !Range(flashIntensity, 0, 3)) yield return "强化闪光参数超出范围，持续时间不能超过提前量。";
        if (!Range(arrowScale, 1, 2) || !Range(radius, 0, 1) || !Range(pitch, .1f, 6) || !Range(width, .005f, .5f)
            || !Range(length, .1f, 12) || !Range(lifetime, .03f, 2)) yield return "强化箭体或螺旋尺寸超出范围。";
        if (!Range(rotationsPerSecond, -8, 8) || !Range(trailIntensity, 0, 4) || !Range(trailGlow, 0, 2)) yield return "强化螺旋发光或速度超出范围。";
        if (!Range(impactRadius, .5f, 3) || !Range(impactDuration, .2f, 2) || !Range(impactIntensity, 0, 3)
            || !Range(impactGlow, 0, 2) || !Range(impactDistortion, 0, 2) || impactAshCount < 0 || impactAshCount > 128)
            yield return "强化爆炸视觉参数超出范围。";
        foreach (Color color in new[] { arrowColor, trailColor, impactColor })
            if (!Range(color.r, 0, 1) || !Range(color.g, 0, 1) || !Range(color.b, 0, 1) || !Range(color.a, 0, 1))
                yield return "强化箭颜色通道必须为 0～1。";
    }

    //有限闭区间检查与编辑控件保持一致。
    private static bool Range(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;
}
