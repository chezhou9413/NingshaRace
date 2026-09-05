using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;

namespace NingshaRaceLib.PocketMaps.Cargo.UI
{
    //类职责：把原版货运分组模型呈现为可搜索、可直接输入数量的凝砂清单，不改变货运分组规则。
    internal sealed class NingshaCargoListPanel
    {
        private readonly List<TransferableOneWay> visible = new List<TransferableOneWay>();
        private List<TransferableOneWay> source;
        private Vector2 scroll;
        private string search = "";
        private string appliedSearch;
        private bool animals;

        //函数职责：更换来源模型或页签时重建当前过滤结果，并保持其他页签已选择的数量。
        public void Bind(List<TransferableOneWay> values, bool showAnimals)
        {
            if (source == values && animals == showAnimals) return;
            source = values;
            animals = showAnimals;
            Refilter();
        }

        //函数职责：组合检索栏、结果摘要和滚动条目，行高度根据可用宽度自动选择单排或双排。
        public void Draw(Rect area)
        {
            NingshaLayout layout = new NingshaLayout(area);
            search = NingshaInput.Search(layout.Take(NingshaLayout.RowHeight()), search, "cargo:search", "检索货物、动物或定义…");
            if (search != appliedSearch) Refilter();
            Rect outRect = layout.Remaining;
            float width = outRect.width - 18f;
            bool compact = width < 620f;
            float textHeight = NingshaLayout.RowHeight(GameFont.Small, 0f) + NingshaLayout.RowHeight(GameFont.Tiny, 0f);
            float rowHeight = textHeight + 16f + (compact ? NingshaLayout.RowHeight() + 8f : 0f);
            Rect view = new Rect(0f, 0f, width, Mathf.Max(outRect.height, visible.Count * (rowHeight + 8f)));
            Widgets.BeginScrollView(outRect, ref scroll, view);
            try
            {
                for (int i = 0; i < visible.Count; i++)
                {
                    Rect row = new Rect(0f, i * (rowHeight + 8f), width, rowHeight);
                    if (row.yMax < scroll.y || row.y > scroll.y + outRect.height) continue;
                    DrawRow(row, visible[i], compact);
                }
                if (visible.Count == 0)
                    NingshaText.Label(new Rect(0f, 0f, width, NingshaLayout.RowHeight()), "当前没有符合条件的可发送条目。", NingshaPalette.Muted);
            }
            finally { Widgets.EndScrollView(); }
        }

        //函数职责：绘制条目图标、名称、库存和数量操作区，选中条目以绿松石刻线标记。
        private static void DrawRow(Rect row, TransferableOneWay item, bool compact)
        {
            NingshaFrame.Panel(row, inset: true);
            if (item.CountToTransfer > 0)
                Widgets.DrawBoxSolid(new Rect(row.x + 4f, row.y + 8f, 2f, row.height - 16f), NingshaPalette.Jade);
            float controlsHeight = NingshaLayout.RowHeight();
            Rect controls = compact
                ? new Rect(row.x + 10f, row.yMax - controlsHeight - 8f, row.width - 20f, controlsHeight)
                : new Rect(row.xMax - 272f, row.center.y - controlsHeight / 2f, 262f, controlsHeight);
            Rect labelArea = new Rect(row.x + 54f, row.y + 8f,
                (compact ? row.xMax - 10f : controls.x - 8f) - row.x - 54f, Text.LineHeightOf(GameFont.Small) + 2f);
            Widgets.ThingIcon(new Rect(row.x + 12f, row.y + 10f, 32f, 32f), item.AnyThing);
            NingshaText.Label(labelArea, item.Label);
            NingshaText.Label(new Rect(labelArea.x, labelArea.yMax, labelArea.width, Text.LineHeightOf(GameFont.Tiny) + 2f),
                "可发送 " + item.MaxCount + " · 已选 " + item.CountToTransfer, NingshaPalette.Muted, GameFont.Tiny);
            TooltipHandler.TipRegion(new Rect(row.x, row.y, labelArea.xMax - row.x, labelArea.yMax - row.y), item.TipDescription);
            DrawCountControls(controls, item);
        }

        //函数职责：用原版可调整模型处理减量、键盘输入、增量、全选和清空，不越过库存上限。
        private static void DrawCountControls(Rect rect, TransferableOneWay item)
        {
            string key = "cargo:" + item.AnyThing.thingIDNumber;
            Rect minus = NingshaLayout.Column(rect, 0, 5, 4f);
            Rect input = NingshaLayout.Column(rect, 1, 5, 4f);
            if (NingshaButton.Draw(minus, "－", key + ":minus", item.Interactive && item.CountToTransfer > 0)) item.AdjustBy(-1);
            using (new NingshaGuiScope(GameFont.Small))
            {
                GUI.enabled = item.Interactive;
                string buffer = NingshaInput.TextField(input, item.EditBuffer, key + ":count");
                item.EditBuffer = buffer;
                if (int.TryParse(buffer, out int amount) && amount >= 0 && amount <= item.MaxCount)
                {
                    if (amount != item.CountToTransfer) item.AdjustTo(amount);
                }
                else
                {
                    NingshaFrame.Border(input, NingshaPalette.Danger);
                    TooltipHandler.TipRegion(input, "请输入 0 到 " + item.MaxCount + " 之间的整数。");
                }
            }
            if (NingshaButton.Draw(NingshaLayout.Column(rect, 2, 5, 4f), "＋", key + ":plus", item.Interactive && item.CountToTransfer < item.MaxCount)) item.AdjustBy(1);
            bool invalid = !int.TryParse(item.EditBuffer, out int edited) || edited < 0 || edited > item.MaxCount;
            if (NingshaButton.Draw(NingshaLayout.Column(rect, 3, 5, 4f), "全部", key + ":all", item.Interactive && (item.CountToTransfer < item.MaxCount || invalid))) item.AdjustTo(item.MaxCount);
            if (NingshaButton.Draw(NingshaLayout.Column(rect, 4, 5, 4f), "清空", key + ":none", item.Interactive && (item.CountToTransfer > 0 || invalid))) item.AdjustTo(0);
        }

        //函数职责：检查全部页签输入，防止确认时忽略无效或未完成的数量编辑。
        public static bool HasInvalidAmount(List<TransferableOneWay> items)
        {
            foreach (TransferableOneWay item in items)
            {
                if (!int.TryParse(item.EditBuffer, out int amount) || amount < 0 || amount > item.MaxCount) return true;
            }
            return false;
        }

        //函数职责：按页签和检索关键字过滤当前分组，检索变化时回到列表顶端。
        private void Refilter()
        {
            visible.Clear();
            foreach (TransferableOneWay item in source)
            {
                if ((item.ThingDef.category == ThingCategory.Pawn) != animals) continue;
                if (item.Label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || item.ThingDef.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) visible.Add(item);
            }
            appliedSearch = search;
            scroll = Vector2.zero;
        }
    }
}
