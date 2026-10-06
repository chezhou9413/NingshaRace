using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //提供中文参数分组、数值输入和滑条，细节默认折叠。
    internal sealed class BladebearerParameterPanel
    {
        private readonly Window_BladebearerEditor editor;
        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        private Vector2 scroll;
        private float contentHeight = 1000, width, y;
        private bool flowDetails, breakupDetails, colorDetails, particleDetails;
        private string selectedId;
        private float Row => Mathf.Max(30, Text.LineHeight + 8);

        //保存编辑会话，字段变化只通知当前窗口。
        internal BladebearerParameterPanel(Window_BladebearerEditor editor) { this.editor = editor; }

        //导入或恢复参数后清除输入缓存，防止旧文本覆盖新值。
        internal void ResetInputs() { buffers.Clear(); GUI.FocusControl(null); }

        //按可用高度滚动，不占用窗口底部导入导出区。
        internal void Draw(Rect rect)
        {
            BladebearerPart p = editor.Selected;
            if (p == null) return;
            if (selectedId != p.id) { selectedId = p.id; buffers.Clear(); scroll = Vector2.zero; colorDetails = p.particle; }
            width = rect.width - 20; y = 0;
            Widgets.BeginScrollView(rect, ref scroll, new Rect(0, 0, width, contentHeight));
            try
            {
                Label((p.particle ? "独立粒子 · " : "部件 · ") + p.label);
                string label = Widgets.TextField(Next(), p.label);
                if (label != p.label) { p.label = label; editor.Changed(); }
                if (Widgets.ButtonText(Next(), "适用朝向：" + FacingLabel(p.facing))) ChooseFacing(p);
                if (Widgets.ButtonText(Next(), "绑定：" + (p.attachment == "Weapon" ? "武器握持" : "身体"))) ChooseAttachment(p);
                if (p.flame) DrawFlow(p);
                if (p.particle) DrawParticles(p);
                if (!p.particle && p.additive > 0) DrawStarlight(p);
                Label(p.attachment == "Weapon" ? "原版武器局部位置与大小" : "位置与大小");
                Pair("position", "左右位置", "上下位置", ref p.position, -5, 5);
                Pair("size", "画布宽度", "画布高度", ref p.size, 0.01f, 10);
                Number("angle", "部件整体旋转（°）", ref p.angle, -180, 180);
                if (Number("layer", "前后层级", ref p.layer, p.attachment == "Weapon" ? 0 : -10, p.attachment == "Weapon" ? 20 : 100)) editor.Owner.Rebuild();
                if (!p.particle && Widgets.ButtonText(Next(), "贴图：" + p.texture.Split('/').Last())) ChooseTexture(p);
                if (Widgets.ButtonText(Next(), (colorDetails ? "▼ " : "▶ ")
                    + (p.particle ? "颜色与透明度" : "底色、透明度与实体深度"))) colorDetails = !colorDetails;
                if (colorDetails)
                {
                    Tint("color", ref p.color);
                    if (Number("additive", "光束叠加比例", ref p.additive, 0, 1)) editor.Owner.Rebuild();
                    if (!p.particle)
                    {
                        bool depth = p.writeDepth;
                        Widgets.CheckboxLabeled(Next(), "实体轮廓写入深度", ref p.writeDepth);
                        if (depth != p.writeDepth) editor.Changed(true);
                    }
                }
                if (!p.particle)
                {
                    if (!p.flame) DrawFlow(p);
                    Label("置换破碎");
                    Number("breakup", "破碎强度", ref p.breakup, 0, 2);
                    if (Widgets.ButtonText(Next(), (breakupDetails ? "▼ " : "▶ ") + "破碎细节")) breakupDetails = !breakupDetails;
                    if (breakupDetails) Pair("breakupScale", "横向破碎密度", "纵向破碎密度", ref p.breakupScale, 0.01f, 12);
                }
                if (p.HasBloom)
                {
                    Label("后处理辉光");
                    Number("bloomIntensity", "辉光强度", ref p.bloomIntensity, 0, 2);
                    Number("bloomThreshold", "亮部阈值", ref p.bloomThreshold, 0, 1);
                }
                if (Widgets.ButtonText(Next(), "恢复此部件初始参数")) { editor.ResetSelected(); buffers.Clear(); }
                contentHeight = y + 8;
            }
            finally { Widgets.EndScrollView(); }
        }

        //星光在基础亮度上柔和起伏，幅度为零时保持常亮。
        private void DrawStarlight(BladebearerPart p)
        {
            Label("星光与闪烁");
            Number("lightIntensity", "基础发光亮度", ref p.lightIntensity, 0, 4);
            if (!p.flame)
            {
                Number("lightFlickerStrength", "闪烁幅度（0 为常亮）", ref p.lightFlickerStrength, 0, 0.5f);
                Number("lightFlickerFrequency", "闪烁速度（次/秒）", ref p.lightFlickerFrequency, 0.1f, 5);
            }
        }

        //粒子发射与火焰置换分开，根部、尾端和方向箭头仍可在预览里拖动。
        private void DrawParticles(BladebearerPart p)
        {
            Label("持续发射粒子");
            string source = string.IsNullOrEmpty(p.particleFlame) ? "独立节奏" : editor.Owner.ParticleFlame(p).label;
            string button = "跟随火焰：" + source;
            float height = Mathf.Max(Row, Text.CalcHeight(button, width - 20) + 10);
            Rect sourceRect = new Rect(0, y, width, height); y += height + 5;
            if (Widgets.ButtonText(sourceRect, button)) ChooseParticleFlame(p);
            TooltipHandler.TipRegion(sourceRect, "共用所选火焰的置换图、传播速度和相位；粒子位置、方向和大小仍独立调整。");
            Number("flowAngle", "飘散偏转角（°）", ref p.flowAngle, -180, 180);
            string hint = "拖动预览绿点设置发射位置，拖青色箭头设置飘散方向；橙点设置方向基准。方向变化用于新发射的火星。";
            float hintHeight = Text.CalcHeight(hint, width) + 8;
            Widgets.Label(new Rect(0, y, width, hintHeight), hint); y += hintHeight + 5;
            Number("particleRate", "每秒数量", ref p.particleRate, 0, 60);
            Number("particleSpeed", "飘散速度", ref p.particleSpeed, 0, 0.8f);
            Number("particleLifetime", "存活秒数", ref p.particleLifetime, 0.1f, 3);
            Number("particleSize", "粒子大小", ref p.particleSize, 0.001f, 0.08f);
            Number("lightIntensity", "火星亮度", ref p.lightIntensity, 0, 4);
            Number("particleSpread", "散开角度（°）", ref p.particleSpread, 0, 180);
            Number("particleSway", "噪声路径扭动幅度", ref p.particleSway, 0, 0.1f);
            if (Widgets.ButtonText(Next(), "使用主体紫色"))
            { p.color = new Color(0.7f, 0.38f, 1, p.color.a); buffers.Clear(); editor.Changed(); }
            if (Widgets.ButtonText(Next(), (particleDetails ? "▼ " : "▶ ") + "发射与路径细节")) particleDetails = !particleDetails;
            if (!particleDetails) return;
            Pair("root", "发射点 X", "发射点 Y", ref p.root, 0, 1);
            Pair("tip", "方向点 X", "方向点 Y", ref p.tip, 0, 1);
            Number("particleRadius", "发射范围", ref p.particleRadius, 0, 0.2f);
            if (string.IsNullOrEmpty(p.particleFlame))
            {
                Number("speed", "波动速度", ref p.speed, 0, 6);
                Number("phase", "动画相位", ref p.phase, -100, 100);
            }
        }

        //粒子可以跟随当前草稿内的任意火焰，选择结果随 XML 一起交付。
        private void ChooseParticleFlame(BladebearerPart p)
        {
            var options = editor.Owner.Draft.Where(part => part.flame && !part.particle && part.attachment == p.attachment
                    && (part.facing == "All" || part.facing == p.facing))
                .Select(part => new FloatMenuOption(part.label, () => { p.particleFlame = part.id; editor.Changed(true); })).ToList();
            options.Add(new FloatMenuOption("独立节奏", () => { p.particleFlame = ""; editor.Changed(true); }));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        //火焰的角度放在常用参数首位，不要求美工先编辑根部、尾端坐标。
        private void DrawFlow(BladebearerPart p)
        {
            Label("飘动方向与幅度");
            Number("flowAngle", "飘散偏转角（°）", ref p.flowAngle, -180, 180);
            string tip = "0° 沿根部→尾端；正值逆时针，负值顺时针。青色箭头可直接拖动。";
            float tipHeight = Text.CalcHeight(tip, width) + 8;
            Widgets.Label(new Rect(0, y, width, tipHeight), tip); y += tipHeight + 5;
            Number("speed", "传播速度", ref p.speed, 0, 6);
            Pair("displacement", "横向摆幅", "纵向摆幅", ref p.displacement, -0.4f, 0.4f);
            if (Widgets.ButtonText(Next(), "置换图：" + p.displacementTexture)) ChooseNoise(p);
            if (Widgets.ButtonText(Next(), (flowDetails ? "▼ " : "▶ ") + "根部、尾端与细节")) flowDetails = !flowDetails;
            if (!flowDetails) return;
            Pair("root", "根部 X", "根部 Y", ref p.root, 0, 1);
            Pair("tip", "尾端 X", "尾端 Y", ref p.tip, 0, 1);
            Number("rootLock", "根部固定比例", ref p.rootLock, 0, 0.8f);
            Number("bendPower", "尾端位移曲线", ref p.bendPower, 0.5f, 4);
            Pair("noiseScale", "横向细节密度", "纵向波动密度", ref p.noiseScale, 0.01f, 12);
            Number("detailStrength", "细节扰动比例", ref p.detailStrength, 0, 1);
            Number("phase", "动画相位", ref p.phase, -100, 100);
        }

        //选择已交付的原图，避免美工手写资源路径。
        private void ChooseTexture(BladebearerPart p)
        {
            Find.WindowStack.Add(new FloatMenu(editor.Owner.Definition.parts
                .Select(part => new FloatMenuOption(part.label, () => { p.texture = part.texture; editor.Changed(); })).ToList()));
        }
        //选择本 Mod 已注册的置换贴图。
        private void ChooseNoise(BladebearerPart p)
        {
            Find.WindowStack.Add(new FloatMenu(BladebearerResources.NoiseNames()
                .Select(name => new FloatMenuOption(name, () => { p.displacementTexture = name; editor.Changed(); })).ToList()));
        }

        //朝向值保持英文枚举名，界面使用中文。
        internal static string FacingLabel(string facing)
        {
            switch (facing)
            {
                case "South": return "正面";
                case "East": return "右侧";
                case "West": return "左侧";
                case "North": return "背面";
                default: return "全部";
            }
        }

        //切换部件可见方向后重建请求，避免旧朝向残留。
        private void ChooseFacing(BladebearerPart p)
        {
            Find.WindowStack.Add(new FloatMenu(new[] { "South", "East", "West", "North", "All" }
                .Select(value => new FloatMenuOption(FacingLabel(value), () =>
                {
                    p.facing = value; editor.Select(p.id); editor.Changed(true);
                    if (p.particle && !string.IsNullOrEmpty(p.particleFlame)
                        && editor.Owner.ParticleFlame(p).facing != "All" && editor.Owner.ParticleFlame(p).facing != p.facing)
                        editor.Status = "粒子朝向已改变，请在“跟随火焰”中选择同朝向火焰后导出。";
                })).ToList()));
        }

        //绑定变化同时解除跨坐标系的火焰跟随关系，局部参数保留供美工调整。
        private void ChooseAttachment(BladebearerPart p)
        {
            Find.WindowStack.Add(new FloatMenu(new[] { "Body", "Weapon" }.Select(value =>
                new FloatMenuOption(value == "Weapon" ? "武器握持" : "身体", () =>
                {
                    if (p.attachment == value) return;
                    p.attachment = value;
                    if (value == "Weapon") p.layer = Mathf.Clamp(p.layer, 0, 20);
                    p.particleFlame = "";
                    foreach (BladebearerPart particle in editor.Owner.Draft.Where(part => part.particleFlame == p.id))
                        particle.particleFlame = "";
                    buffers.Clear(); editor.Changed(true);
                })).ToList()));
        }
        //每组用足够行高的标题分隔。
        private void Label(string text) { y += 8; Widgets.Label(Next(), text); }
        //分配一行并留出控件间距。
        private Rect Next() { var r = new Rect(0, y, width, Row); y += Row + 5; return r; }
        //坐标分量使用同一种数值控件。
        private void Pair(string key, string x, string z, ref Vector2 value, float min, float max)
        { Number(key + ".x", x, ref value.x, min, max); Number(key + ".y", z, ref value.y, min, max); }
        //颜色使用直观的 RGB 和透明度滑条。
        private void Tint(string key, ref Color value)
        {
            Number(key + ".r", "红", ref value.r, 0, 1); Number(key + ".g", "绿", ref value.g, 0, 1);
            Number(key + ".b", "蓝", ref value.b, 0, 1);
            Number(key + ".a", "透明度", ref value.a, 0, 1);
        }

        //文字和数值各占一行，滑条独立一行，避免中文挤压输入框。
        private bool Number(string key, string label, ref float value, float min, float max)
        {
            float before = value;
            Rect row = new Rect(0, y, width, Mathf.Max(Row, Text.CalcHeight(label, width - 96) + 8));
            y += row.height + 5;
            Widgets.Label(new Rect(row.x, row.y, row.width - 96, row.height), label);
            string control = "Bladebearer_" + selectedId + "_" + key;
            if (!buffers.ContainsKey(key) || GUI.GetNameOfFocusedControl() != control)
                buffers[key] = value.ToString("R", CultureInfo.InvariantCulture);
            GUI.SetNextControlName(control);
            Rect field = new Rect(row.xMax - 92, row.y, 92, row.height);
            string text = Widgets.TextField(field, buffers[key]); buffers[key] = text;
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                && !float.IsNaN(parsed) && !float.IsInfinity(parsed) && parsed >= min && parsed <= max) value = parsed;
            else
            {
                Color old = GUI.color; GUI.color = new Color(1, 0.4f, 0.3f); Widgets.DrawBox(field); GUI.color = old;
                TooltipHandler.TipRegion(field, "请输入 " + min + " 到 " + max + " 之间的数字；红框内容尚未应用。");
            }
            Rect slider = new Rect(0, y, width, 22); y += 27;
            float sliderValue = Widgets.HorizontalSlider(slider, value, min, max, true);
            if (sliderValue != value) { value = sliderValue; buffers[key] = value.ToString("R", CultureInfo.InvariantCulture); }
            if (before == value) return false;
            editor.Changed(); return true;
        }
    }
}
