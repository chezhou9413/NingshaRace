using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;
using NingshaRaceLib.UI.Windows;

namespace NingshaRaceLib.DevTools.DesertPit
{
    //类职责：把凝砂摆放目录组合成可检索、可折叠的古遗迹条目面板，保留地图摆放工具入口。
    public class Dialog_NingshaDevPlacement : NingshaWindow
    {
        private readonly List<NingshaDevPlacementEntry> entries;
        private readonly List<NingshaDevPlacementEntry> filtered = new List<NingshaDevPlacementEntry>();
        private readonly HashSet<string> collapsed = new HashSet<string>();
        private Vector2 scrollPosition;
        private string search = "";
        private string appliedSearch;
        private string selectedDef;

        //属性职责：提供受屏幕约束的初始窗口尺寸和调试窗口标识。
        public override Vector2 InitialSize => new Vector2(Mathf.Min(560f, Verse.UI.screenWidth), Mathf.Min(680f, Verse.UI.screenHeight));
        public override bool IsDebug => true;

        //构造职责：初始化目录、拖动和关闭规则，不占用地图摄像机操作。
        public Dialog_NingshaDevPlacement()
        {
            draggable = true;
            resizeable = false;
            closeOnAccept = false;
            closeOnCancel = true;
            preventCameraMotion = false;
            entries = NingshaDevPlacementCatalog.CreateEntries();
            UpdateFilter();
        }

        //函数职责：组合窗口壳、检索栏、结果数和滚动目录，并保护全部界面状态。
        public override void DoWindowContents(Rect inRect)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Rect area = DrawShell(inRect, "凝砂摆放工具", "选择条目后点击或拖拽摆放；右键或取消键退出摆放。");
                NingshaLayout layout = new NingshaLayout(area);
                search = NingshaInput.Search(layout.Take(NingshaLayout.RowHeight()), search, "placement:search");
                if (search != appliedSearch) UpdateFilter();
                NingshaText.Label(layout.Take(NingshaLayout.RowHeight(GameFont.Tiny, 4f)), filtered.Count + " 项可选内容 · 点击分类展开或收起", NingshaPalette.Muted, GameFont.Tiny);
                DrawList(layout.Remaining);
            }
        }

        //函数职责：只在检索条件变化时重新过滤条目，匹配中文名称、定义和分类。
        private void UpdateFilter()
        {
            filtered.Clear();
            foreach (NingshaDevPlacementEntry entry in entries)
            {
                if (entry.Label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || entry.DefName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || entry.Category.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    filtered.Add(entry);
            }
            appliedSearch = search;
            scrollPosition = Vector2.zero;
        }

        //函数职责：按当前折叠状态测量列表，并在可滚动区域绘制同一套行布局。
        private void DrawList(Rect rect)
        {
            float rowHeight = NingshaLayout.RowHeight();
            float headerHeight = NingshaLayout.RowHeight(GameFont.Small, 14f);
            float height = 0f;
            string category = null;
            foreach (NingshaDevPlacementEntry entry in filtered)
            {
                if (category != entry.Category) { height += headerHeight + 6f; category = entry.Category; }
                if (!collapsed.Contains(category)) height += rowHeight + 5f;
            }
            Rect view = new Rect(0f, 0f, rect.width - 18f, Mathf.Max(rect.height, height));
            Widgets.BeginScrollView(rect, ref scrollPosition, view);
            try
            {
                float y = 0f;
                category = null;
                foreach (NingshaDevPlacementEntry entry in filtered)
                {
                    if (category != entry.Category)
                    {
                        category = entry.Category;
                        bool folded = collapsed.Contains(category);
                        if (NingshaButton.Draw(new Rect(0f, y, view.width, headerHeight), (folded ? "＋ " : "－ ") + category,
                            "placement:category:" + category, selected: !folded))
                        {
                            if (folded) collapsed.Remove(category); else collapsed.Add(category);
                            break;
                        }
                        y += headerHeight + 6f;
                    }
                    if (collapsed.Contains(category)) continue;
                    Rect row = new Rect(8f, y, view.width - 8f, rowHeight);
                    if (NingshaButton.Draw(row, entry.Label, "placement:" + entry.DefName,
                        tip: entry.Label + "\n" + entry.DefName, selected: selectedDef == entry.DefName))
                    {
                        SelectEntry(entry);
                        selectedDef = entry.DefName;
                    }
                    y += rowHeight + 5f;
                }
                if (filtered.Count == 0)
                    NingshaText.Label(new Rect(0f, 0f, view.width, rowHeight), "没有找到符合条件的内容。", NingshaPalette.Muted);
            }
            finally { Widgets.EndScrollView(); }
        }

        //函数职责：根据条目创建地图摆放工具，地图不存在时明确拒绝操作。
        private static void SelectEntry(NingshaDevPlacementEntry entry)
        {
            if (Find.CurrentMap == null)
            {
                Messages.Message("当前没有可摆放的地图。", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            BuildableDef buildableDef = entry.ResolveDef();
            Find.DesignatorManager.Select(new Designator_NingshaDevPlace(buildableDef));
        }
    }
}
