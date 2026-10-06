using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //单个震尼实体的美术编辑窗口，关闭后恢复默认外观，参数通过 XML 交付。
    public sealed class Window_BladebearerEditor : Window
    {
        internal readonly CompBladebearerAppearance Owner;
        internal readonly BladebearerPreview Preview;
        private readonly BladebearerParameterPanel parameters;
        private readonly BladebearerPartsPanel parts;
        private readonly Dictionary<string, BladebearerPart> initial = new Dictionary<string, BladebearerPart>();
        private string selectedId, fileName, status = "调整只临时作用于当前实体；关闭窗口恢复默认。";
        private bool dirty, discardConfirmed, compactParameters = true;
        private float lastClock;
        internal BladebearerPart Selected => Owner.Draft?.FirstOrDefault(p => p.id == selectedId);
        internal string Status { set { status = value; } }
        private float Row => Mathf.Max(32, Text.LineHeight + 8);
        public override Vector2 InitialSize => new Vector2(Mathf.Min(1380, Verse.UI.screenWidth - 24), Mathf.Min(920, Verse.UI.screenHeight - 24));

        //构造控件时不修改 Pawn，窗口正式打开后再创建临时草稿。
        public Window_BladebearerEditor(CompBladebearerAppearance owner)
        {
            Owner = owner; Preview = new BladebearerPreview(this);
            fileName = owner.Pawn.def.label + "外观";
            parameters = new BladebearerParameterPanel(this); parts = new BladebearerPartsPanel(this);
            doCloseX = true; closeOnAccept = false; forcePause = true;
            absorbInputAroundWindow = true; preventSave = true; resizeable = false;
        }

        //进入会话时从 Def 复制参数，不修改共享定义。
        public override void PostOpen()
        {
            base.PostOpen(); Owner.Draft = Owner.Definition.CopyParts();
            foreach (BladebearerPart part in Owner.Draft) RememberInitial(part);
            selectedId = Owner.Draft[0].id; Owner.Clock = Time.realtimeSinceStartup;
            lastClock = Time.realtimeSinceStartup; Owner.Rebuild();
        }

        //即使游戏暂停，美工仍可独立播放 Shader 动画。
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (Owner.Pawn.Destroyed || !Owner.Pawn.Spawned) { discardConfirmed = true; Close(); return; }
            float now = Time.realtimeSinceStartup;
            if (!Owner.Paused) Owner.Clock += now - lastClock;
            lastClock = now;
        }

        //相机渲染在 GUI 控件之前执行，避免占用当前滑条的热控件。
        public override void WindowOnGUI()
        {
            if (Owner.Draft != null && !Owner.Pawn.Destroyed) Preview.BeforeWindowGUI();
            base.WindowOnGUI();
        }

        //大窗口使用三列，小窗口把部件与参数切为侧栏页签。
        public override void DoWindowContents(Rect rect)
        {
            GameFont font = Text.Font; TextAnchor anchor = Text.Anchor; bool wrap = Text.WordWrap; Color color = GUI.color;
            try
            {
                Text.Font = GameFont.Small; Text.Anchor = TextAnchor.MiddleLeft; Text.WordWrap = true; GUI.color = Color.white;
                float row = Row;
                Widgets.Label(new Rect(0, 0, rect.width - 52, row), Owner.Pawn.def.label + "外观编辑器" + (dirty ? "  ·  未导出" : ""));
                float footerHeight = row * 2 + 14 + Text.CalcHeight(BladebearerPresetXml.DirectoryPath, rect.width - 100) + 8;
                Rect body = new Rect(0, row + 10, rect.width, Mathf.Max(80, rect.height - row - 20 - footerHeight));
                if (rect.width >= 1050)
                {
                    float left = 208, right = 330;
                    parts.Draw(new Rect(body.x, body.y, left, body.height));
                    Preview.Draw(new Rect(left + 12, body.y, body.width - left - right - 24, body.height));
                    parameters.Draw(new Rect(body.xMax - right, body.y, right, body.height));
                }
                else
                {
                    float side = Mathf.Min(330, body.width * 0.48f), x = body.xMax - side;
                    if (Widgets.ButtonText(new Rect(x, body.y, (side - 6) / 2, row), "部件列表")) compactParameters = false;
                    if (Widgets.ButtonText(new Rect(x + (side + 6) / 2, body.y, (side - 6) / 2, row), "参数调整")) compactParameters = true;
                    Rect panel = new Rect(x, body.y + row + 8, side, body.height - row - 8);
                    if (compactParameters) parameters.Draw(panel); else parts.Draw(panel);
                    Preview.Draw(new Rect(body.x, body.y, body.width - side - 12, body.height));
                }
                DrawFooter(new Rect(0, body.yMax + 10, rect.width, footerHeight));
            }
            finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; }
        }

        //固定保留文件名、交付操作、目录和状态，不被参数滚动挤出窗口。
        private void DrawFooter(Rect rect)
        {
            float row = Row, buttonWidth = 100, x = rect.xMax - 3 * (buttonWidth + 6);
            Widgets.Label(new Rect(rect.x, rect.y, 58, row), "文件名");
            fileName = Widgets.TextField(new Rect(rect.x + 62, rect.y, Mathf.Max(50, x - 70), row), fileName);
            if (Widgets.ButtonText(new Rect(x, rect.y, buttonWidth, row), "导出 XML")) Export();
            if (Widgets.ButtonText(new Rect(x + buttonWidth + 6, rect.y, buttonWidth, row), "导入 XML")) ChooseImport();
            if (Widgets.ButtonText(new Rect(x + 2 * (buttonWidth + 6), rect.y, buttonWidth, row), "关闭")) Close();
            float pathHeight = Text.CalcHeight(BladebearerPresetXml.DirectoryPath, rect.width - 100) + 8;
            Rect path = new Rect(rect.x, rect.y + row + 6, rect.width - 100, pathHeight);
            Widgets.Label(path, BladebearerPresetXml.DirectoryPath);
            if (Widgets.ButtonText(new Rect(rect.xMax - 94, path.y, 94, row), "复制目录")) GUIUtility.systemCopyBuffer = BladebearerPresetXml.DirectoryPath;
            Rect message = new Rect(rect.x, path.yMax + 4, rect.width, row);
            Text.WordWrap = false; Widgets.Label(message, status); Text.WordWrap = true;
            TooltipHandler.TipRegion(message, status);
        }

        //切换部件时同步单独查看目标，空选择用于当前朝向没有粒子的情况。
        internal void Select(string id)
        {
            parameters.ResetInputs();
            selectedId = id;
            if (Selected != null && Selected.facing != "All") Preview.Facing = Rot4.FromString(Selected.facing);
            if (Owner.SoloId != null) { Owner.SoloId = id; Owner.Rebuild(); }
            Preview.Invalidate();
        }

        //四向切换优先选中对应部件，找不到对应项时选择该方向可见的同类部件。
        internal void SetFacing(Rot4 facing)
        {
            if (Preview.Facing == facing) return;
            BladebearerPart current = Selected;
            Preview.Facing = facing;
            if (current != null && !current.VisibleAt(facing))
            {
                string suffix = "_" + current.facing;
                string role = current.id.EndsWith(suffix, StringComparison.Ordinal)
                    ? current.id.Substring(0, current.id.Length - suffix.Length) : current.id;
                string id = facing == Rot4.South ? role : role + "_" + facing.ToStringWord();
                var visible = Owner.Draft.Where(p => p.VisibleAt(facing) && (!parts.OnlyParticles || p.particle));
                BladebearerPart next = visible.FirstOrDefault(p => p.id == id)
                    ?? visible.FirstOrDefault(p => p.particle == current.particle && p.attachment == current.attachment)
                    ?? visible.FirstOrDefault();
                Select(next?.id);
            }
            else if (current == null)
                Select(Owner.Draft.FirstOrDefault(p => p.VisibleAt(facing) && (!parts.OnlyParticles || p.particle))?.id);
            parts.ResetScroll(); Preview.Invalidate();
        }

        //新建后直接进入粒子列表，方便继续添加或调整方向。
        internal void ShowParticles() { parts.ShowParticles(); }
        //数值变化只刷新预览，结构、显隐或层级变化才重建渲染树。
        internal void Changed(bool rebuild = false)
        { dirty = true; if (rebuild) Owner.Rebuild(); Preview.Invalidate(); }
        //保留新建副本或刚导入配置的初始状态，支持单部件复位。
        internal void RememberInitial(BladebearerPart part) { initial[part.id] = part.Copy(); }
        //只恢复当前部件，其余部件的调整保留。
        internal void ResetSelected()
        {
            parameters.ResetInputs();
            BladebearerPart selected = Selected;
            BladebearerPart restored = initial[selected.id].Copy();
            if (restored.particle && !string.IsNullOrEmpty(restored.particleFlame)
                && !Owner.Draft.Any(p => p.id == restored.particleFlame && p.flame && !p.particle))
            {
                status = "无法恢复：初始配置跟随的火焰「" + restored.particleFlame + "」已经删除。";
                return;
            }
            Owner.Draft[Owner.Draft.IndexOf(selected)] = restored; Select(restored.id); Changed(true);
        }

        //已有文件必须由美工明确选择覆盖。
        private void Export()
        {
            string path = Path.Combine(BladebearerPresetXml.DirectoryPath, fileName + ".xml");
            if (File.Exists(path)) Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("覆盖已有参数文件？\n" + path, Save));
            else Save();
        }
        //成功写盘后标记当前草稿已经交付。
        private void Save()
        {
            RunFileAction(() =>
            {
                string path = BladebearerPresetXml.Export(fileName, Owner.Definition, Owner.Draft); dirty = false;
                status = "已导出：" + path;
            });
        }
        //导入列表仅包含参数目录中的 XML 文件。
        private void ChooseImport()
        {
            RunFileAction(() =>
            {
                Directory.CreateDirectory(BladebearerPresetXml.DirectoryPath);
                var files = Directory.GetFiles(BladebearerPresetXml.DirectoryPath, "*.xml").OrderBy(p => p).ToList();
                if (files.Count == 0) { status = "参数目录中还没有 XML；可先导出，或把已有参数文件复制到此目录。"; return; }
                Find.WindowStack.Add(new FloatMenu(files.Select(path => new FloatMenuOption(Path.GetFileName(path), () => ConfirmImport(path))).ToList()));
            });
        }
        //替换有改动的草稿前给美工保留返回导出的机会。
        private void ConfirmImport(string path)
        {
            if (dirty) Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("导入将替换当前页尚未导出的草稿，继续？", () => Import(path)));
            else Import(path);
        }
        //解析成功后一次性替换，失败时不更改当前部件。
        private void Import(string path)
        {
            RunFileAction(() =>
            {
                List<BladebearerPart> imported = BladebearerPresetXml.Import(path, Owner.Definition.defName);
                Owner.Draft = imported; initial.Clear(); foreach (BladebearerPart p in imported) RememberInitial(p);
                parameters.ResetInputs();
                Owner.SoloId = null; parts.ShowAll();
                Select((imported.FirstOrDefault(p => p.VisibleAt(Preview.Facing)) ?? imported[0]).id);
                fileName = Path.GetFileNameWithoutExtension(path);
                Changed(true); dirty = false; status = "已导入：" + path;
            });
        }
        //仅处理实际文件、格式和参数错误，其他编程错误仍交给游戏日志。
        private void RunFileAction(Action action)
        {
            try { action(); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is XmlException || e is InvalidOperationException || e is ArgumentException)
            { status = e.Message; Find.WindowStack.Add(new Dialog_MessageBox("参数文件操作失败：\n" + e.Message)); }
        }

        //提醒美工导出尚未保存的调整。
        public override bool OnCloseRequest()
        {
            if (!dirty || discardConfirmed) return true;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("当前调整尚未导出。关闭后将恢复默认外观，仍然关闭？", () => { discardConfirmed = true; Close(); }));
            return false;
        }
        //无论从何处关闭，都释放预览资源并恢复当前 Pawn。
        public override void PostClose()
        { Preview.Dispose(); Owner.EndEditing(); base.PostClose(); }
    }
}
