using System;
using System.IO;
using System.Linq;
using System.Xml;
using NingshaRaceLib.Erosion.Utility;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Editor
{
    //选蛇头、选朝向后直接调整；预览、参数和导出固定分区。
    public sealed class Window_ErosionSnakeEditor : Window
    {
        internal readonly ErosionSnakeEditorSession Session;
        private readonly ErosionSnakePreview preview;
        private readonly ErosionSnakeParameterPanel parameters;
        private int selectedIndex;
        private bool dirty, discardConfirmed;
        private string status = "调整为临时预览；导出 XML 保留参数，关闭后角色恢复原样。";
        internal Rot4 Facing = Rot4.South;
        internal ErosionSnakeDraft Selected => Session.Snakes[selectedIndex];
        internal static float Row => Mathf.Max(32, Text.LineHeightOf(GameFont.Small) + 8);
        public override Vector2 InitialSize => new Vector2(Mathf.Min(1120, Verse.UI.screenWidth - 24), Mathf.Min(840, Verse.UI.screenHeight - 24));

        //只接受地图上的侵蚀体，关闭窗口不会向存档留下参数。
        public Window_ErosionSnakeEditor(Pawn pawn)
        {
            Session = new ErosionSnakeEditorSession(pawn);
            preview = new ErosionSnakePreview(this); parameters = new ErosionSnakeParameterPanel(this);
            Session.Preview = preview;
            doCloseX = true; closeOnAccept = false; forcePause = true;
            absorbInputAroundWindow = true; preventSave = true;
        }

        //打开后才安装独立节点草稿。
        public override void PostOpen() { base.PostOpen(); Session.Begin(); }

        //预览时钟独立于暂停的游戏，目标消失时清理会话。
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (Session.Pawn.Destroyed || !Session.Pawn.Spawned || !ErosionPawnUtility.IsNingshaErosionBody(Session.Pawn))
            { discardConfirmed = true; Close(); return; }
            Session.UpdateClock();
        }

        //相机在窗口控件之前绘制，保留正确的 GUI 输入顺序。
        public override void WindowOnGUI() { preview.BeforeWindowGUI(); base.WindowOnGUI(); }

        //左右分区随屏幕尺寸缩放，参数通过滚动适应较小窗口。
        public override void DoWindowContents(Rect rect)
        {
            GameFont font = Text.Font; TextAnchor anchor = Text.Anchor; bool wrap = Text.WordWrap; Color color = GUI.color;
            try
            {
                Text.Font = GameFont.Small; Text.Anchor = TextAnchor.MiddleLeft; Text.WordWrap = true; GUI.color = Color.white;
                Widgets.Label(new Rect(0, 0, rect.width - 48, Row), "侵蚀体 · 蛇头编辑器" + (dirty ? "  ·  未导出" : ""));
                float y = Row + 6;
                DrawSnakeTabs(new Rect(0, y, rect.width, Row)); y += Row + 6;
                DrawFacingTabs(new Rect(0, y, rect.width, Row)); y += Row + 10;
                float statusHeight = Text.CalcHeight(status, rect.width) + 8;
                float footerHeight = Row + statusHeight + 8;
                Rect body = new Rect(0, y, rect.width, Mathf.Max(40, rect.height - y - footerHeight - 10));
                float right = Mathf.Min(390, body.width * 0.46f);
                preview.Draw(new Rect(0, body.y, body.width - right - 14, body.height));
                parameters.Draw(new Rect(body.xMax - right, body.y, right, body.height));
                DrawFooter(new Rect(0, body.yMax + 10, rect.width, footerHeight), statusHeight);
            }
            finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; }
        }

        //蛇头切换保留当前朝向，方便比较三个部件。
        private void DrawSnakeTabs(Rect row)
        {
            float width = (row.width - 6 * (Session.Snakes.Count - 1)) / Session.Snakes.Count;
            for (int i = 0; i < Session.Snakes.Count; i++)
            {
                if (!Widgets.ButtonText(new Rect(i * (width + 6), row.y, width, row.height),
                    (i == selectedIndex ? "● " : "") + Session.Snakes[i].Source.debugLabel)) continue;
                selectedIndex = i; parameters.ResetInputs(); preview.Invalidate();
            }
        }

        //四向参数互不覆盖，方向名称兼顾人物视图和地图方向。
        private void DrawFacingTabs(Rect row)
        {
            Rot4[] facings = { Rot4.South, Rot4.East, Rot4.West, Rot4.North };
            string[] names = { "正面 · 南", "右侧 · 东", "左侧 · 西", "背面 · 北" };
            float width = (row.width - 18) / 4;
            for (int i = 0; i < 4; i++)
            {
                if (!Widgets.ButtonText(new Rect(i * (width + 6), row.y, width, row.height),
                    (Facing == facings[i] ? "● " : "") + names[i])) continue;
                Facing = facings[i]; parameters.ResetInputs(); preview.Invalidate();
            }
        }

        //交付操作固定在底部，目录可直接复制到文件管理器。
        private void DrawFooter(Rect area, float statusHeight)
        {
            float width = (area.width - 18) / 4;
            if (Widgets.ButtonText(new Rect(0, area.y, width, Row), "导出 XML")) Export();
            if (Widgets.ButtonText(new Rect(width + 6, area.y, width, Row), "导入 XML")) ChooseImport();
            if (Widgets.ButtonText(new Rect(2 * (width + 6), area.y, width, Row), "复制目录"))
            { GUIUtility.systemCopyBuffer = ErosionSnakePresetXml.DirectoryPath; status = "已复制参数目录，可粘贴到文件管理器地址栏。"; }
            if (Widgets.ButtonText(new Rect(3 * (width + 6), area.y, width, Row), "关闭")) Close();
            Widgets.Label(new Rect(0, area.y + Row + 8, area.width, statusHeight), status);
        }

        //参数变化只更新当前角色与预览。
        internal void Changed() { dirty = true; Session.Changed(); preview.Invalidate(); }

        //每次导出使用独立文件名，不覆盖已有美术参数。
        private void Export()
        {
            FileAction(() =>
            {
                string path = ErosionSnakePresetXml.Export(Session); dirty = false;
                status = "已导出：" + Path.GetFileName(path) + "。可放入模组 1.6/Patches 使用。";
            });
        }

        //从固定参数目录选取本工具导出的文件。
        private void ChooseImport()
        {
            FileAction(() =>
            {
                Directory.CreateDirectory(ErosionSnakePresetXml.DirectoryPath);
                string[] files = Directory.GetFiles(ErosionSnakePresetXml.DirectoryPath, "*.xml").OrderByDescending(path => path).ToArray();
                if (files.Length == 0) { status = "参数目录中没有 XML；可先导出，或把参数文件复制到此目录。"; return; }
                Find.WindowStack.Add(new FloatMenu(files.Select(path => new FloatMenuOption(Path.GetFileName(path), () => ConfirmImport(path))).ToList()));
            });
        }

        //覆盖未导出的调整前留出取消机会。
        private void ConfirmImport(string path)
        {
            Action import = () => FileAction(() =>
            {
                ErosionSnakePresetXml.Import(path, Session); dirty = false;
                parameters.ResetInputs(); preview.Invalidate(); status = "已导入：" + Path.GetFileName(path);
            });
            if (dirty) Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("导入将替换尚未导出的调整，继续？", import));
            else import();
        }

        //文件访问或参数格式错误向用户说明，不吞掉程序错误。
        private void FileAction(Action action)
        {
            try { action(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is XmlException
                || error is InvalidOperationException || error is ArgumentException || error is FormatException || error is OverflowException)
            { status = "参数文件操作失败：" + error.Message; Find.WindowStack.Add(new Dialog_MessageBox(status)); }
        }

        //关窗前提醒保留尚未导出的美术调整。
        public override bool OnCloseRequest()
        {
            if (!dirty || discardConfirmed) return true;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("还有未导出的蛇头调整，关闭后将恢复原样。仍要关闭？",
                () => { discardConfirmed = true; Close(); }));
            return false;
        }

        //任意关闭路径都恢复角色并释放预览资源。
        public override void PostClose()
        {
            try { preview.Dispose(); }
            finally { Session.End(); base.PostClose(); }
        }
    }
}
