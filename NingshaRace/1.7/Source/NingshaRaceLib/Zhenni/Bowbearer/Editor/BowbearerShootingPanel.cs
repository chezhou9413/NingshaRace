using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//射击参数按动作、箭体和尾迹分组，中文标签和滑条分别占行。
internal sealed partial class BowbearerShootingPanel
{
    private readonly Window_BowbearerShooting editor;
    private readonly Dictionary<string, string> buffers = new();
    private readonly HashSet<string> invalid = new();
    private Vector2 scroll;
    private float height = 1000, y, width;
    private int tab;
    private float Row => Mathf.Max(30, Text.LineHeight + 8);
    internal bool HasInvalidInputs => invalid.Count > 0;

    //控件只修改当前草稿。
    internal BowbearerShootingPanel(Window_BowbearerShooting editor) { this.editor = editor; }

    //导入或恢复后清除文本缓存。
    internal void Reset() { buffers.Clear(); invalid.Clear(); scroll = Vector2.zero; GUI.FocusControl(null); }

    //参数区严格限制在底部交付按钮之上。
    internal void Draw(Rect area)
    {
        string[] tabs = { "拉弓", "箭头", "箭尾", "螺旋", "灰烬", "强化" };
        float button = (area.width - (tabs.Length - 1) * 6) / tabs.Length;
        for (int i = 0; i < tabs.Length; i++)
            if (Widgets.ButtonText(new Rect(area.x + i * (button + 6), area.y, button, Row), (tab == i ? "●" : "") + tabs[i]))
            { tab = i; Reset(); }
        area.yMin += Row + 8;
        width = area.width - 20; y = 0;
        Widgets.BeginScrollView(area, ref scroll, new Rect(0, 0, width, height));
        try
        {
            BowbearerSettings s = editor.Owner.Draft;
            if (tab == 0) Animation(s);
            else if (tab == 3) Spiral(s);
            else if (tab == 4) AshAndImpact(s);
            else if (tab == 5) Empowered(s.empowered);
            else Arrow(tab == 1 ? s.arrowHead : s.arrowTail);
            height = y + 12;
        }
        finally { Widgets.EndScrollView(); }
    }

    //动作阶段与握持美术参数独立于真实武器伤害。
    private void Animation(BowbearerSettings s)
    {
        Label("动作帧映射（0 为原版，1～3 为差分）");
        Frame("待机", s.frame0, value => s.frame0 = value);
        Frame("拉弦／蓄力保持", s.frame1, value => s.frame1 = value);
        Frame("发射首帧", s.frame2, value => s.frame2 = value);
        Frame("发射末帧／回位", s.frame3, value => s.frame3 = value);
        Number("releaseSeconds", "发射首帧时长（秒）", ref s.releaseSeconds, .01f, 1);
        Number("recoverySeconds", "发射末帧时长（秒）", ref s.recoverySeconds, .01f, 2);
        Number("releaseAngle", "释放偏转（度）", ref s.releaseAngle, -45, 45);
        Label("共用握点与四向握持");
        Pair("grip", "握点 UV", ref s.grip, 0, 1);
        Pair("handSouth", "正面握持", ref s.handSouth, -3, 3);
        Pair("handEast", "右侧握持", ref s.handEast, -3, 3);
        Pair("handWest", "左侧握持", ref s.handWest, -3, 3);
        Pair("handNorth", "背面握持", ref s.handNorth, -3, 3);
        Number("angleSouth", "正面校正角", ref s.angleSouth, -180, 180);
        Number("angleEast", "右侧校正角", ref s.angleEast, -180, 180);
        Number("angleWest", "左侧校正角", ref s.angleWest, -180, 180);
        Number("angleNorth", "背面校正角", ref s.angleNorth, -180, 180);
        Pair("muzzle", "放箭偏移（横向／前方）", ref s.muzzle, -3, 3);
    }

    //螺旋形状、速度、寿命与发光均能即时预览。
    private void Spiral(BowbearerSettings s)
    {
        Pair("trailAnchor", "拖尾衔接点（箭尾 UV）", ref s.trailAnchor, 0, 1);
        Number("radius", "螺旋半径（格）", ref s.radius, 0, 1);
        Number("pitch", "每圈前进距离（格）", ref s.pitch, .1f, 6);
        Number("rotationsPerSecond", "旋转速度（圈／秒，负数反转）", ref s.rotationsPerSecond, -8, 8);
        Number("width", "火焰带宽度（格）", ref s.width, .005f, .5f);
        Number("length", "最大拖尾长度（格）", ref s.length, .1f, 12);
        Number("lifetime", "尾迹寿命（秒）", ref s.lifetime, .03f, 2);
        Number("taper", "渐细与消散曲线", ref s.taper, .1f, 4);
        Number("joinBlend", "箭尾合拢／末端融合长度（格）", ref s.joinBlend, .05f, 1);
        Number("joinOverlap", "与箭体重叠渐融长度（格）", ref s.joinOverlap, .05f, .8f);
        Number("distortion", "火焰扰动", ref s.distortion, 0, 2);
        Number("trailSpeed", "火焰流动速度", ref s.trailSpeed, 0, 6);
        Number("trailIntensity", "尾迹亮度", ref s.trailIntensity, 0, 4);
        Number("trailGlow", "柔光强度", ref s.trailGlow, 0, 2);
        Tint("trailColor", ref s.trailColor);
    }

    //箭体脱落灰烬与命中爆开共用预览和 XML 参数。
    private void AshAndImpact(BowbearerSettings s)
    {
        Label("飞行灰烬");
        Number("arrowAshRate", "发射数量（个／秒）", ref s.arrowAshRate, 0, 240);
        Number("arrowAshLifetime", "灰烬寿命（秒）", ref s.arrowAshLifetime, .1f, 2);
        Number("arrowAshSize", "灰烬大小（格）", ref s.arrowAshSize, .001f, .16f);
        Number("arrowAshSpeed", "飘散速度（格／秒）", ref s.arrowAshSpeed, 0, 3);
        Tint("ashColor", ref s.ashColor);
        Label("命中爆焰（纯视觉，无额外伤害）");
        Number("impactRadius", "爆开半径（格）", ref s.impactRadius, .1f, 2);
        Number("impactDuration", "爆焰时长（秒）", ref s.impactDuration, .1f, 2);
        Number("impactIntensity", "爆焰亮度", ref s.impactIntensity, 0, 4);
        Number("impactGlow", "爆焰辉光强度", ref s.impactGlow, 0, 2);
        Number("impactDistortion", "爆焰翻卷扰动", ref s.impactDistortion, 0, 2);
        float count = s.impactAshCount;
        Number("impactAshCount", "命中灰烬数量", ref count, 0, 128);
        s.impactAshCount = Mathf.RoundToInt(count);
        Tint("impactColor", ref s.impactColor);
    }

    //箭体直接使用与身体火焰相同的参数结构。
    private void Arrow(BladebearerPart p)
    {
        Toggle("显示此部件", ref p.enabled); Toggle("燃烧与自发光", ref p.flame);
        Pair("position", "局部位置", ref p.position, -5, 5);
        Pair("size", "画布大小", ref p.size, .01f, 10);
        Number("angle", "部件角度", ref p.angle, -180, 180);
        Number("layer", "前后层级", ref p.layer, -10, 100);
        Pair("root", "火焰根部", ref p.root, 0, 1);
        Pair("tip", "火焰尾端", ref p.tip, 0, 1);
        Pair("displacement", "横向／纵向摆幅", ref p.displacement, -.4f, .4f);
        Number("flowAngle", "流动方向", ref p.flowAngle, -180, 180);
        Number("rootLock", "根部固定比例", ref p.rootLock, 0, .8f);
        Number("bendPower", "位移曲线", ref p.bendPower, .5f, 4);
        Number("speed", "流动速度", ref p.speed, 0, 6);
        Pair("noiseScale", "噪声密度", ref p.noiseScale, .01f, 12);
        Number("detailStrength", "细节扰动", ref p.detailStrength, 0, 1);
        Number("breakup", "破碎强度", ref p.breakup, 0, 2);
        Pair("breakupScale", "破碎密度", ref p.breakupScale, .01f, 12);
        Number("phase", "火焰相位", ref p.phase, -100, 100);
        Number("additive", "加色比例", ref p.additive, 0, 1);
        Number("lightIntensity", "发光亮度（加色层）", ref p.lightIntensity, 0, 4);
        Tint("color", ref p.color);
    }

    //帧编号通过固定候选选择，避免输入不存在的贴图编号。
    private void Frame(string label, int current, Action<int> set)
    {
        if (!Widgets.ButtonText(Next(), label + "：帧 " + current)) return;
        Find.WindowStack.Add(new FloatMenu(Enumerable.Range(0, 4).Select(i =>
            new FloatMenuOption("帧 " + i, () => { set(i); editor.Changed(); })).ToList()));
    }

    //布尔参数变化也让预览重新计算。
    private void Toggle(string label, ref bool value)
    { bool previous = value; Widgets.CheckboxLabeled(Next(), label, ref value); if (value != previous) editor.Changed(); }

    //分组标签遵循当前字体行高。
    private void Label(string label) { y += 8; Widgets.Label(Next(), label); }

    //每行留出独立间距。
    private Rect Next() { Rect rect = new(0, y, width, Row); y += Row + 6; return rect; }

    //二维坐标统一使用数字和滑条。
    private void Pair(string key, string label, ref Vector2 value, float min, float max)
    { Number(key + ".x", label + " X", ref value.x, min, max); Number(key + ".y", label + " Y", ref value.y, min, max); }

    //颜色保留完整透明通道。
    private void Tint(string key, ref Color value)
    {
        Number(key + ".r", "红色", ref value.r, 0, 1); Number(key + ".g", "绿色", ref value.g, 0, 1);
        Number(key + ".b", "蓝色", ref value.b, 0, 1); Number(key + ".a", "透明度", ref value.a, 0, 1);
    }

    //非法文字保留红框并阻止导出，不静默修正用户输入。
    private void Number(string key, string label, ref float value, float min, float max)
    {
        key = tab + ":" + key;
        float previous = value;
        float rowHeight = Mathf.Max(Row, Text.CalcHeight(label, width - 96) + 8);
        Rect row = new(0, y, width, rowHeight); y += rowHeight + 4;
        Widgets.Label(new Rect(0, row.y, width - 96, rowHeight), label);
        string control = "Bowbearer_" + key;
        if (!buffers.ContainsKey(key) || GUI.GetNameOfFocusedControl() != control && !invalid.Contains(key))
            buffers[key] = value.ToString("R", CultureInfo.InvariantCulture);
        GUI.SetNextControlName(control);
        Rect input = new(width - 92, row.y, 92, rowHeight);
        buffers[key] = Widgets.TextField(input, buffers[key]);
        if (float.TryParse(buffers[key], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            && float.IsFinite(parsed) && parsed >= min && parsed <= max)
        { value = parsed; invalid.Remove(key); }
        else
        {
            invalid.Add(key); Color old = GUI.color; GUI.color = Color.red; Widgets.DrawBox(input); GUI.color = old;
            TooltipHandler.TipRegion(input, $"请输入 {min}～{max} 的有限数字；红框尚未应用。");
        }
        float slider = Widgets.HorizontalSlider(new Rect(0, y, width, 24), value, min, max, true); y += 30;
        if (slider != value)
        { value = slider; invalid.Remove(key); buffers[key] = value.ToString("R", CultureInfo.InvariantCulture); }
        if (value != previous) editor.Changed();
    }
}
