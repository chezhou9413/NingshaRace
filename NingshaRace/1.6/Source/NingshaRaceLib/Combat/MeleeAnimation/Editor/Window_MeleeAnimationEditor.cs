using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //所有接入框架的单位共用编辑窗口，关闭后恢复 Def，参数通过 XML 交付。
    public sealed class Window_MeleeAnimationEditor : Window
    {
        internal readonly CompMeleeAnimation Owner;
        internal readonly MeleeAnimationPreview Preview;
        private readonly MeleeAnimationPanel panel;
        private string fileName, status = "预览不会攻击目标；导出的 XML 可直接替换 Mod 内对应动画 Def。";
        private bool discard;
        private float lastClock;
        private float Row => Mathf.Max(32, Text.LineHeight + 10);
        internal string Status { set => status = value; }
        public override Vector2 InitialSize => new Vector2(Mathf.Min(1180, Verse.UI.screenWidth - 24), Mathf.Min(900, Verse.UI.screenHeight - 24));

        //构造阶段不改动 Pawn，正式打开后再创建草稿。
        public Window_MeleeAnimationEditor(CompMeleeAnimation owner)
        {
            Owner = owner; Preview = new MeleeAnimationPreview(this); panel = new MeleeAnimationPanel(this);
            doCloseX = true; closeOnAccept = false; forcePause = true; preventSave = true; absorbInputAroundWindow = true;
        }

        //每个窗口持有完整独立的动画配置。
        public override void PostOpen()
        {
            base.PostOpen(); Owner.Draft = MeleeAnimationPresetXml.Copy(Owner.Definition);
            fileName = Owner.Definition.defName; Owner.Clock = 0; Owner.Paused = false; lastClock = Time.realtimeSinceStartup;
        }

        //预览时钟独立于被暂停的游戏。
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (!Owner.Pawn.Spawned || Owner.Pawn.Destroyed) { discard = true; Close(); return; }
            float now = Time.realtimeSinceStartup;
            if (!Owner.Paused) Owner.Clock += now - lastClock;
            lastClock = now; panel.Update();
        }

        //相机先于 IMGUI 控件运行，避免渲染过程改变输入焦点。
        public override void WindowOnGUI() { Preview.Render(); base.WindowOnGUI(); }

        //预览和参数各自拥有可用空间，长路径独立换行，不挤占底部按钮。
        public override void DoWindowContents(Rect rect)
        {
            GameFont font = Text.Font; TextAnchor anchor = Text.Anchor; bool wrap = Text.WordWrap; Color color = GUI.color;
            try
            {
                Text.Font = GameFont.Small; Text.Anchor = TextAnchor.MiddleLeft; Text.WordWrap = true; GUI.color = Color.white;
                Widgets.Label(new Rect(0, 0, rect.width - 45, Row), "近战动画编辑器 · " + Owner.Pawn.LabelShort + (panel.Dirty ? " · 未导出" : ""));
                float pathHeight = Text.CalcHeight(MeleeAnimationPresetXml.DirectoryPath, rect.width - 100) + 8;
                float footer = Row * 2 + pathHeight + 26;
                Rect body = new Rect(0, Row + 8, rect.width, Mathf.Max(70, rect.height - Row - 8 - footer));
                float side = Mathf.Min(380, body.width * 0.49f);
                Preview.Draw(new Rect(0, body.y, body.width - side - 12, body.height));
                panel.Draw(new Rect(body.xMax - side, body.y, side, body.height));
                float y = body.yMax + 8, start = rect.width - 312;
                fileName = Widgets.TextField(new Rect(0, y, Mathf.Max(50, start - 8), Row), fileName);
                if (Widgets.ButtonText(new Rect(start, y, 100, Row), "导出 XML")) Export();
                if (Widgets.ButtonText(new Rect(start + 106, y, 100, Row), "导入 XML")) ChooseImport();
                if (Widgets.ButtonText(new Rect(start + 212, y, 100, Row), "关闭")) Close();
                y += Row + 6;
                Widgets.Label(new Rect(0, y, rect.width - 100, pathHeight), MeleeAnimationPresetXml.DirectoryPath);
                if (Widgets.ButtonText(new Rect(rect.width - 94, y, 94, Row), "复制目录")) GUIUtility.systemCopyBuffer = MeleeAnimationPresetXml.DirectoryPath;
                Rect message = new Rect(0, y + pathHeight + 4, rect.width, Row);
                Text.WordWrap = false; Widgets.Label(message, status); TooltipHandler.TipRegion(message, status);
            }
            finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; }
        }

        //覆盖已有文件前确认具体路径。
        private void Export()
        {
            string path = Path.Combine(MeleeAnimationPresetXml.DirectoryPath, fileName + ".xml");
            if (File.Exists(path)) Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("覆盖动画文件？\n" + path, Save));
            else Save();
        }
        //只在成功写盘后清除未导出标记。
        private void Save() => FileAction(() => { status = "已导出：" + MeleeAnimationPresetXml.Export(fileName, Owner.Draft); panel.Dirty = false; });
        //文件选择来自明确的预设目录。
        private void ChooseImport() => FileAction(() =>
        {
            Directory.CreateDirectory(MeleeAnimationPresetXml.DirectoryPath);
            string[] files = Directory.GetFiles(MeleeAnimationPresetXml.DirectoryPath, "*.xml");
            if (files.Length == 0) { status = "目录中暂无 XML；先导出或把动画文件复制到此目录。"; return; }
            Find.WindowStack.Add(new FloatMenu(files.OrderBy(p => p).Select(p => new FloatMenuOption(Path.GetFileName(p), () =>
            {
                if (panel.Dirty) Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("替换尚未导出的动画草稿？", () => Import(p)));
                else Import(p);
            })).ToList()));
        });
        //所有字段通过校验后才应用到草稿。
        private void Import(string path) => FileAction(() =>
        {
            var definition = MeleeAnimationPresetXml.Import(path, Owner.Definition.defName);
            Owner.Draft = definition; panel.Reset(); fileName = Path.GetFileNameWithoutExtension(path);
            Preview.Invalidate(); status = "已导入：" + path;
        });
        //文件与格式错误显示原因，编程异常保留原日志。
        private void FileAction(Action action)
        {
            try { action(); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is XmlException || e is InvalidOperationException || e is ArgumentException)
            { status = e.Message; Find.WindowStack.Add(new Dialog_MessageBox("动画参数操作失败：\n" + e.Message)); }
        }
        //关闭前保护尚未交付的草稿。
        public override bool OnCloseRequest()
        {
            if (!panel.Dirty || discard) return true;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("动画参数尚未导出，仍然关闭？", () => { discard = true; Close(); }));
            return false;
        }
        //窗口释放资源，当前 Pawn 回到运行时配置。
        public override void PostClose()
        {
            Preview.Dispose(); Owner.Draft = null; Owner.PreviewPlayback.Clear(); Owner.Trail.DisposePreview();
            Owner.CapturePreview = false; base.PostClose();
        }
        //配置保留枚举单词，界面使用中文。
        internal static string FacingLabel(string facing)
        { switch (facing) { case "South": return "正面"; case "East": return "右侧"; case "West": return "左侧"; default: return "背面"; } }
    }
}
