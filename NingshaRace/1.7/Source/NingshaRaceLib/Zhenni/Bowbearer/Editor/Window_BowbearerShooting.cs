using System;
using System.IO;
using System.Linq;
using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//独立射击编辑会话，暂停世界并在相机中播放无伤害预览。
public sealed class Window_BowbearerShooting : Window
{
    internal readonly CompBowbearerShooting Owner;
    private readonly BowbearerShootingPreview preview;
    private readonly BowbearerShootingPanel panel;
    private bool playing = true, loop = true, dirty, discardConfirmed;
    private float lastTime, playbackSpeed = .5f;
    private string fileName = "震尼挽弓者射击特效", status = "调整仅作用于草稿，关闭恢复默认；导出 XML 后可交付。";
    private float Row => Mathf.Max(32, Text.LineHeight + 8);
    public override Vector2 InitialSize => new(Mathf.Min(1380, Verse.UI.screenWidth - 24), Mathf.Min(940, Verse.UI.screenHeight - 24));

    //构造时不修改单位，正式打开窗口后创建草稿。
    public Window_BowbearerShooting(CompBowbearerShooting owner)
    {
        Owner = owner; preview = new(owner); panel = new(this);
        doCloseX = true; closeOnAccept = false; forcePause = true; preventSave = true; absorbInputAroundWindow = true;
    }

    //深复制嵌套图层，保护共享 Def。
    public override void PostOpen()
    {
        base.PostOpen(); Owner.Draft = Owner.Definition.settings.Copy(); Owner.ForcedFrame = -1;
        lastTime = UnityEngine.Time.realtimeSinceStartup;
    }

    //预览使用独立时钟，游戏暂停仍可以播放。
    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (!Owner.Ready) { discardConfirmed = true; Close(); return; }
        float now = UnityEngine.Time.realtimeSinceStartup;
        if (playing)
        {
            preview.Time += (now - lastTime) * playbackSpeed;
            if (preview.Time > preview.Duration)
            { if (loop) preview.Time %= preview.Duration; else { preview.Time = preview.Duration; playing = false; } }
        }
        lastTime = now;
    }

    //相机绘制先于滑条和输入框获取控件。
    public override void WindowOnGUI()
    {
        if (Owner.Draft != null && Owner.Ready) preview.Render();
        base.WindowOnGUI();
    }

    //预览与参数两栏，底部始终保留导入导出区域。
    public override void DoWindowContents(Rect rect)
    {
        if (!IsOpen || Owner.Draft == null) return;
        var font = Text.Font; var anchor = Text.Anchor; bool wrap = Text.WordWrap; Color color = GUI.color;
        try
        {
            Text.Font = GameFont.Small; Text.Anchor = TextAnchor.MiddleLeft; Text.WordWrap = true; GUI.color = Color.white;
            Widgets.Label(new Rect(0, 0, rect.width - 48, Row), "震尼挽弓者射击特效编辑器" + (dirty ? " · 未导出" : ""));
            float footerHeight = Row * 3 + 24;
            Rect body = new(0, Row + 8, rect.width, Mathf.Max(80, rect.height - Row - footerHeight - 20));
            float side = Mathf.Min(380, body.width * .43f);
            Rect scene = new(body.x, body.y, body.width - side - 12, body.height);
            DrawPreview(scene);
            panel.Draw(new Rect(body.xMax - side, body.y, side, body.height));
            Footer(new Rect(0, body.yMax + 8, rect.width, footerHeight));
        }
        finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; }
    }

    //播放控制与帧时间轴使用相同的实际蓄力时长。
    private void DrawPreview(Rect rect)
    {
        float bw = (rect.width - 18) / 4;
        if (Widgets.ButtonText(new Rect(rect.x, rect.y, bw, Row), playing ? "暂停" : "播放")) playing = !playing;
        if (Widgets.ButtonText(new Rect(rect.x + bw + 6, rect.y, bw, Row), "单步"))
        { playing = false; preview.Time = Mathf.Min(preview.Duration, preview.Time + 1f / 60); }
        if (Widgets.ButtonText(new Rect(rect.x + (bw + 6) * 2, rect.y, bw, Row), "从头播放")) { preview.Time = 0; playing = true; }
        if (Widgets.ButtonText(new Rect(rect.x + (bw + 6) * 3, rect.y, bw, Row), loop ? "循环：开" : "循环：关")) loop = !loop;
        float y = rect.y + Row + 6;
        Rot4[] faces = { Rot4.South, Rot4.East, Rot4.West, Rot4.North };
        for (int i = 0; i < 4; i++)
            if (Widgets.ButtonText(new Rect(rect.x + (bw + 6) * i, y, bw, Row),
                (preview.Facing == faces[i] ? "●" : "") + BladebearerParameterPanel.FacingLabel(faces[i].ToStringWord())))
            { preview.Facing = faces[i]; preview.Error = null; }
        y += Row + 6;
        if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, Row), preview.Empowered ? "预览：强化箭（点击切换普通箭）" : "预览：普通箭（点击切换强化箭）"))
        { preview.Empowered = !preview.Empowered; preview.Time = 0; preview.Invalidate(); }
        y += Row + 6;
        Widgets.Label(new Rect(rect.x, y, rect.width, Row), $"时间 {preview.Time:F2}/{preview.Duration:F2} 秒 · 蓄力 {preview.Warmup:F2} 秒");
        y += Row;
        float next = Widgets.HorizontalSlider(new Rect(rect.x, y, rect.width, 24), preview.Time, 0, preview.Duration, true); y += 30;
        if (next != preview.Time) { preview.Time = next; playing = false; preview.Error = null; }
        Widgets.Label(new Rect(rect.x, y, rect.width, Row), $"播放速度 {playbackSpeed:F2} 倍（仅预览）"); y += Row;
        playbackSpeed = Widgets.HorizontalSlider(new Rect(rect.x, y, rect.width, 24), playbackSpeed, .05f, 2, true); y += 30;
        if (Widgets.ButtonText(new Rect(rect.x, y, rect.width * .5f - 3, Row), preview.LightBackground ? "深色背景" : "浅色背景")) preview.LightBackground = !preview.LightBackground;
        if (Widgets.ButtonText(new Rect(rect.x + rect.width * .5f + 3, y, rect.width * .5f - 3, Row), "恢复预览视角")) { preview.Zoom = .25f; preview.Error = null; }
        y += Row + 6;
        preview.Draw(new Rect(rect.x, y, rect.width, Mathf.Max(32, rect.yMax - y)));
    }

    //文件操作不随参数滚动，路径可复制。
    private void Footer(Rect rect)
    {
        float fieldWidth = Mathf.Max(120, rect.width - 440);
        fileName = Widgets.TextField(new Rect(rect.x, rect.y, fieldWidth, Row), fileName);
        float x = rect.x + fieldWidth + 8;
        if (Widgets.ButtonText(new Rect(x, rect.y, 100, Row), "导出 XML")) Export();
        if (Widgets.ButtonText(new Rect(x + 108, rect.y, 100, Row), "导入 XML")) ChooseImport();
        if (Widgets.ButtonText(new Rect(x + 216, rect.y, 100, Row), "恢复默认"))
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("恢复将丢弃当前草稿，继续？", () =>
            { Owner.Draft = Owner.Definition.settings.Copy(); panel.Reset(); Changed(); }));
        if (Widgets.ButtonText(new Rect(x + 324, rect.y, 100, Row), "复制目录")) GUIUtility.systemCopyBuffer = BowbearerPresetXml.DirectoryPath;
        Text.WordWrap = false;
        Rect info = new(rect.x, rect.y + Row + 6, rect.width, Row);
        Widgets.Label(info, status); TooltipHandler.TipRegion(info, status);
        Rect path = new(rect.x, info.yMax + 4, rect.width, Row);
        Widgets.Label(path, BowbearerPresetXml.DirectoryPath); TooltipHandler.TipRegion(path, BowbearerPresetXml.DirectoryPath);
    }

    //参数变化后允许重新渲染，并标记尚未交付。
    internal void Changed() { dirty = true; preview.Invalidate(); }

    //文件名与参数先检查，覆盖既有交付文件时明确确认。
    private void Export()
    {
        try
        {
            if (panel.HasInvalidInputs) throw new InvalidOperationException("请先修正红框输入。");
            BowbearerPresetXml.Validate(Owner.Draft);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName is "." or "..")
                throw new InvalidOperationException("请输入合法文件名。");
            if (File.Exists(Path.Combine(BowbearerPresetXml.DirectoryPath, fileName + ".xml")))
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("覆盖同名射击预设？", Save));
            else Save();
        }
        catch (Exception e) { status = "导出失败：" + e.Message; }
    }

    //确认后保存当前完整草稿。
    private void Save()
    {
        try { status = "已导出：" + BowbearerPresetXml.Export(fileName, Owner.Definition, Owner.Draft); dirty = false; }
        catch (Exception e) { status = "导出失败：" + e.Message; }
    }

    //只枚举专属目录内的 XML，跨单位文件在解析阶段拒绝。
    private void ChooseImport()
    {
        try
        {
            if (!Directory.Exists(BowbearerPresetXml.DirectoryPath)) { status = "尚无导出的射击预设。"; return; }
            string[] files = Directory.GetFiles(BowbearerPresetXml.DirectoryPath, "*.xml");
            if (files.Length == 0) { status = "尚无导出的射击预设。"; return; }
            Find.WindowStack.Add(new FloatMenu(files.OrderBy(p => p).Select(path => new FloatMenuOption(Path.GetFileName(path), () =>
            {
                if (dirty) Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("导入将替换当前草稿，继续？", () => Import(path)));
                else Import(path);
            })).ToList()));
        }
        catch (Exception e) { status = "读取预设目录失败：" + e.Message; }
    }

    //导入失败保留原草稿。
    private void Import(string path)
    {
        try
        {
            BowbearerSettings imported = BowbearerPresetXml.Import(path, Owner.Definition.defName);
            Owner.Draft = imported; panel.Reset(); Changed(); status = "已导入：" + Path.GetFileName(path);
        }
        catch (Exception e) { status = "导入失败：" + e.Message; }
    }

    //未导出修改由用户选择是否丢弃。
    public override void Close(bool doCloseSound = true)
    {
        if (dirty && !discardConfirmed)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("关闭将丢弃尚未导出的射击参数，继续？", () =>
            { discardConfirmed = true; Close(doCloseSound); }));
            return;
        }
        base.Close(doCloseSound);
    }

    //退出后恢复默认参数，移除预览回调与资源。
    public override void PostClose()
    {
        preview.Dispose(); Owner.Draft = null; Owner.ForcedFrame = -1;
        base.PostClose();
    }
}
