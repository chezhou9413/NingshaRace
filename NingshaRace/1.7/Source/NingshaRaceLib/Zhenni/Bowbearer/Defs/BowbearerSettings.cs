using System.Collections.Generic;
using System.Reflection;
using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//运行时与编辑器使用同一套数值；箭体图层沿用现有火焰参数。
public sealed class BowbearerSettings
{
    public int frame0, frame1 = 1, frame2 = 2, frame3 = 3;
    public float releaseSeconds = .12f, recoverySeconds = .2f;
    public float releaseAngle = 8f;
    public Vector2 grip = new(.4402174f, .2565217f);
    public Vector2 handSouth = new(.35f, .05f), handEast = new(.4f, .05f);
    public Vector2 handWest = new(-.4f, .05f), handNorth = new(.35f, .05f);
    public float angleSouth, angleEast, angleWest, angleNorth;
    public Vector2 muzzle = new(0, .35f);
    public float radius = .18f, pitch = 1.2f, width = .1f, length = 4f;
    public float lifetime = .3f, rotationsPerSecond = 1.5f, taper = 1f;
    public float joinBlend = .4f, joinOverlap = .3f;
    public Vector2 trailAnchor = new(.82f, .52f);
    public float trailIntensity = 1f, distortion = .65f, trailSpeed = 1f;
    public float trailGlow = 1.2f;
    public Color trailColor = new(.7f, .38f, 1f, 1f);
    public float arrowAshRate = 90f, arrowAshLifetime = .85f, arrowAshSize = .0126f, arrowAshSpeed = .65f;
    public Color ashColor = new(.7f, .58f, .85f, 1f);
    public float impactRadius = .8f, impactDuration = .55f, impactIntensity = .85f;
    public float impactGlow = .3f, impactDistortion = .85f;
    public int impactAshCount = 36;
    public Color impactColor = new(.8f, .4f, 1f, 1f);
    public BladebearerPart arrowHead = new(), arrowTail = new();
    public BowbearerEmpoweredSettings empowered = new();
    internal bool Empowered;

    //两种编辑器草稿不共享嵌套的箭体参数。
    public BowbearerSettings Copy()
    {
        var result = (BowbearerSettings)MemberwiseClone();
        result.arrowHead = arrowHead.Copy(); result.arrowTail = arrowTail.Copy();
        result.empowered = empowered.Copy();
        return result;
    }

    //四向握持使用角色局部坐标。
    internal Vector2 Hand(Verse.Rot4 facing) => facing.AsInt switch
    { 0 => handNorth, 1 => handEast, 2 => handSouth, _ => handWest };

    //四向角度只校正美术朝向，不改变真实弹道。
    internal float Angle(Verse.Rot4 facing) => facing.AsInt switch
    { 0 => angleNorth, 1 => angleEast, 2 => angleSouth, _ => angleWest };

    //第一张差分保持拉弦，成功放箭后依次播放后两张差分，再回到原版。
    internal int Frame(float progress, float sinceRelease)
    {
        if (progress >= 0) return frame1;
        if (sinceRelease < 0) return frame0;
        if (sinceRelease < releaseSeconds) return frame2;
        return sinceRelease < releaseSeconds + recoverySeconds ? frame3 : frame0;
    }

    //导入前检查实际可编辑输入，防止 NaN 和无限尾迹进入绘制。
    internal IEnumerable<string> Errors()
    {
        foreach (string error in empowered.Errors()) yield return error;
        foreach (FieldInfo field in typeof(BowbearerSettings).GetFields())
        {
            object value = field.GetValue(this);
            if (value is float f && !float.IsFinite(f)) yield return field.Name + " 必须是有限数值。";
            if (value is Vector2 v && (!float.IsFinite(v.x) || !float.IsFinite(v.y))) yield return field.Name + " 必须是有限坐标。";
        }
        if (frame0 < 0 || frame0 > 3 || frame1 < 0 || frame1 > 3 || frame2 < 0 || frame2 > 3 || frame3 < 0 || frame3 > 3)
            yield return "帧编号必须为 0～3。";
        if (!Range(releaseSeconds, .01f, 1) || !Range(recoverySeconds, .01f, 2)) yield return "发射首帧或末帧时长超出范围。";
        if (!Range(radius, 0, 1) || !Range(pitch, .1f, 6) || !Range(width, .005f, .5f)
            || !Range(length, .1f, 12) || !Range(lifetime, .03f, 2) || !Range(taper, .1f, 4)) yield return "螺旋尺寸或寿命超出范围。";
        if (!Range(rotationsPerSecond, -8, 8) || !Range(trailIntensity, 0, 4) || !Range(distortion, 0, 2)
            || !Range(trailSpeed, 0, 6) || !Range(trailGlow, 0, 2)) yield return "螺旋速度或发光参数超出范围。";
        if (!Range(trailColor.r, 0, 1) || !Range(trailColor.g, 0, 1) || !Range(trailColor.b, 0, 1) || !Range(trailColor.a, 0, 1)) yield return "螺旋颜色通道必须为 0～1。";
        if (!Range(joinBlend, .05f, 1)) yield return "螺旋衔接长度必须为 0.05～1 格。";
        if (!Range(joinOverlap, .05f, .8f)) yield return "箭尾重叠长度必须为 0.05～0.8 格。";
        if (!Range(trailAnchor.x, 0, 1) || !Range(trailAnchor.y, 0, 1)) yield return "拖尾衔接点必须位于箭尾画布内。";
        if (!Range(arrowAshRate, 0, 240) || !Range(arrowAshLifetime, .1f, 2)
            || !Range(arrowAshSize, .001f, .16f) || !Range(arrowAshSpeed, 0, 3)) yield return "箭矢灰烬参数超出范围。";
        if (!Range(impactRadius, .1f, 2) || !Range(impactDuration, .1f, 2) || !Range(impactIntensity, 0, 4)
            || impactAshCount < 0 || impactAshCount > 128) yield return "命中爆焰参数超出范围。";
        if (!Range(impactGlow, 0, 2) || !Range(impactDistortion, 0, 2)) yield return "命中辉光或扰动必须为 0～2。";
        foreach (Color c in new[] { ashColor, impactColor })
            if (!Range(c.r, 0, 1) || !Range(c.g, 0, 1) || !Range(c.b, 0, 1) || !Range(c.a, 0, 1)) yield return "灰烬和爆焰颜色通道必须为 0～1。";
        if (!Range(grip.x, 0, 1) || !Range(grip.y, 0, 1)) yield return "握点必须位于画布内。";
        foreach (var p in new[] { handSouth, handEast, handWest, handNorth, muzzle })
            if (!Range(p.x, -3, 3) || !Range(p.y, -3, 3)) yield return "握持或放箭偏移必须为 -3～3。";
        foreach (float a in new[] { angleSouth, angleEast, angleWest, angleNorth, releaseAngle })
            if (!Range(a, -180, 180)) yield return "角度必须为 -180～180。";
        foreach (string error in BladebearerValidation.Errors(new() { arrowHead, arrowTail })) yield return error;
        if (arrowHead != null && arrowTail != null && (arrowHead.particle || arrowTail.particle)) yield return "箭体必须是图像层，不能设为粒子发射器。";
    }

    //闭区间检查也排除非有限值。
    private static bool Range(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;
}
