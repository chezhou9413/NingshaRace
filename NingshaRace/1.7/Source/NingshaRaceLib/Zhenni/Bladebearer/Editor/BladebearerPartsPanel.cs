using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //部件列表提供显隐、单独查看与层级调整，不暴露渲染实现细节。
    internal sealed class BladebearerPartsPanel
    {
        private readonly Window_BladebearerEditor editor;
        private Vector2 scroll;
        private Vector2 compactScroll;
        private bool onlyParticles;
        internal bool OnlyParticles => onlyParticles;
        private float Row => Mathf.Max(32, Text.LineHeight + 8);

        //绑定窗口当前的外观草稿。
        internal BladebearerPartsPanel(Window_BladebearerEditor editor) { this.editor = editor; }

        //切换朝向后从列表顶部开始，避免沿用上一面的滚动位置。
        internal void ResetScroll() { scroll = Vector2.zero; compactScroll = Vector2.zero; }
        //创建粒子后只显示发射器，参数区保持当前新建项。
        internal void ShowParticles() { onlyParticles = true; ResetScroll(); }
        //导入整套外观时重新显示全部可见部件。
        internal void ShowAll() { onlyParticles = false; ResetScroll(); }

        //列表独立滚动，底部始终保留常用操作。
        internal void Draw(Rect rect)
        {
            float minimum = Row * 9 + 50;
            if (rect.height >= minimum) { DrawContents(rect); return; }
            Rect content = new Rect(0, 0, rect.width - 20, minimum);
            Widgets.BeginScrollView(rect, ref compactScroll, content);
            try { DrawContents(content); }
            finally { Widgets.EndScrollView(); }
        }

        //在内部可用矩形中摆放列表和操作按钮。
        private void DrawContents(Rect rect)
        {
            float row = Row, footer = row * 4 + 24, header = row * 3 + 18;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, row), "当前朝向：" + BladebearerParameterPanel.FacingLabel(editor.Preview.Facing.ToStringWord()));
            float filterWidth = (rect.width - 6) / 2;
            string allLabel = (onlyParticles ? "" : "● ") + (rect.width < 220 ? "全部" : "全部部件");
            string particleLabel = (onlyParticles ? "● " : "") + (rect.width < 220 ? "粒子" : "仅看粒子");
            if (Widgets.ButtonText(new Rect(rect.x, rect.y + row + 6, filterWidth, row), allLabel)) ShowAll();
            if (Widgets.ButtonText(new Rect(rect.x + filterWidth + 6, rect.y + row + 6, filterWidth, row), particleLabel))
            {
                ShowParticles();
                if (editor.Selected?.particle != true)
                    editor.Select(editor.Owner.Draft.FirstOrDefault(p => p.particle && p.VisibleAt(editor.Preview.Facing))?.id);
            }
            if (Widgets.ButtonText(new Rect(rect.x, rect.y + 2 * (row + 6), rect.width, row), "新增紫色粒子"))
                BladebearerParticleEditing.ChooseSource(editor);
            Rect listRect = new Rect(rect.x, rect.y + header, rect.width, Mathf.Max(40, rect.height - footer - header));
            var ordered = editor.Owner.Draft.Where(p => p.VisibleAt(editor.Preview.Facing) && (!onlyParticles || p.particle))
                .OrderByDescending(p => p.layer).ToList();
            float labelWidth = listRect.width - 62, totalHeight = 0;
            var heights = new float[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                string text = ordered[i].label + "  [" + ordered[i].layer.ToString("0.#") + "]";
                heights[i] = Mathf.Max(row, Text.CalcHeight(text, labelWidth - 8) + 8);
                totalHeight += heights[i] + 5;
            }
            string empty = "当前朝向没有粒子，点击“新增紫色粒子”创建。";
            float emptyHeight = Text.CalcHeight(empty, listRect.width - 28) + 8;
            Widgets.BeginScrollView(listRect, ref scroll, new Rect(0, 0, listRect.width - 20,
                Mathf.Max(listRect.height, ordered.Count == 0 ? emptyHeight : totalHeight)));
            try
            {
                float itemY = 0;
                if (ordered.Count == 0) Widgets.Label(new Rect(0, 0, listRect.width - 28, emptyHeight), empty);
                for (int i = 0; i < ordered.Count; i++)
                {
                    BladebearerPart p = ordered[i];
                    Rect item = new Rect(0, itemY, listRect.width - 20, heights[i]); itemY += heights[i] + 5;
                    if (editor.Selected == p) Widgets.DrawBoxSolid(item, new Color(0.22f, 0.25f, 0.4f));
                    Rect label = new Rect(item.x, item.y, item.width - 42, item.height);
                    string text = p.label + "  [" + p.layer.ToString("0.#") + "]";
                    if (Widgets.ButtonText(label, text, false)) editor.Select(p.id);
                    TooltipHandler.TipRegion(label, p.label + " · " + BladebearerParameterPanel.FacingLabel(p.facing)
                        + " · " + (p.attachment == "Weapon" ? "武器" : "身体") + "\n" + p.texture);
                    bool visible = p.enabled;
                    Widgets.CheckboxLabeled(new Rect(item.xMax - 28, item.y, 28, item.height), "", ref p.enabled);
                    if (visible != p.enabled) editor.Changed(true);
                }
            }
            finally { Widgets.EndScrollView(); }
            float y = listRect.yMax + 8;
            BladebearerPart selected = editor.Selected;
            if (selected == null) return;
            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, row), editor.Owner.SoloId == null ? "单独查看选中部件" : "显示全部部件"))
            {
                editor.Owner.SoloId = editor.Owner.SoloId == null ? selected.id : null;
                editor.Owner.Rebuild(); editor.Preview.Invalidate();
            }
            y += row + 5;
            float half = (rect.width - 6) / 2;
            if (Widgets.ButtonText(new Rect(rect.x, y, half, row), "前移一层")) Move(selected, 1);
            if (Widgets.ButtonText(new Rect(rect.x + half + 6, y, half, row), "后移一层")) Move(selected, -1);
            y += row + 5;
            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, row), selected.particle ? "复制选中粒子" : "复制选中火焰",
                active: selected.flame || selected.particle))
            {
                BladebearerPart copy = selected.Copy();
                copy.id = selected.id + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                copy.label = selected.label + " 副本"; copy.position.x = Mathf.Clamp(copy.position.x + 0.12f, -5, 5);
                editor.Owner.Draft.Add(copy); editor.RememberInitial(copy); editor.Select(copy.id); editor.Changed(true);
            }
            y += row + 5;
            bool duplicate = editor.Owner.Draft.Count > 1 && editor.Owner.Definition.parts.All(p => p.id != selected.id);
            bool followed = editor.Owner.Draft.Any(p => p.particle && p.particleFlame == selected.id);
            Rect deleteRect = new Rect(rect.x, y, rect.width, row);
            if (followed) TooltipHandler.TipRegion(deleteRect, "有粒子正在跟随这个火焰，请先更换粒子的跟随火焰。");
            if (Widgets.ButtonText(deleteRect, selected.particle ? "删除新增粒子" : "删除部件副本", active: duplicate && !followed))
            {
                editor.Owner.Draft.Remove(selected);
                editor.Select(editor.Owner.Draft.FirstOrDefault(p => p.VisibleAt(editor.Preview.Facing) && (!onlyParticles || p.particle))?.id);
                editor.Changed(true);
            }
        }

        //交换相邻部件的层级，相同层级时保留稳定列表次序并拉开间距。
        private void Move(BladebearerPart part, int direction)
        {
            var ordered = editor.Owner.Draft.Where(p => p.attachment == part.attachment && p.facing == part.facing).OrderBy(p => p.layer).ToList();
            int index = ordered.IndexOf(part), target = index + direction;
            if (target < 0 || target >= ordered.Count) return;
            BladebearerPart other = ordered[target];
            float layer = part.layer;
            part.layer = other.layer;
            other.layer = layer;
            if (part.layer == other.layer) part.layer = Mathf.Clamp(part.layer + direction * 0.5f,
                part.attachment == "Weapon" ? 0 : -10, part.attachment == "Weapon" ? 20 : 100);
            editor.Changed(true);
        }
    }
}
