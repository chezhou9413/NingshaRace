using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DevTools
{
    //类职责：显示凝砂开发者摆放工具窗口，并把条目选择转换为地图摆放 Designator。
    public class Dialog_NingshaDevPlacement : Window
    {
        //字段职责：记录窗口内可选择的全部摆放条目。
        private readonly List<NingshaDevPlacementEntry> entries;

        //字段职责：记录滚动列表当前位置。
        private Vector2 scrollPosition;

        //属性职责：声明窗口初始尺寸。
        public override Vector2 InitialSize => new Vector2(460f, 640f);

        //属性职责：标记本窗口属于调试窗口。
        public override bool IsDebug => true;

        //构造函数职责：初始化窗口状态和条目列表。
        public Dialog_NingshaDevPlacement()
        {
            doCloseX = true;
            draggable = true;
            resizeable = true;
            closeOnAccept = false;
            closeOnCancel = true;
            preventCameraMotion = false;
            optionalTitle = "凝砂摆放工具";
            entries = NingshaDevPlacementCatalog.CreateEntries();
        }

        //函数职责：绘制窗口内容和可摆放条目按钮。
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 42f), "选择条目后在地图上点击或拖拽摆放。右键或取消键退出摆放。");
            Rect listRect = new Rect(inRect.x, inRect.y + 48f, inRect.width, inRect.height - 48f);
            float viewHeight = CalculateViewHeight(listRect.width);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            DrawEntries(viewRect);
            Widgets.EndScrollView();
        }

        //函数职责：计算滚动视图实际内容高度。
        private float CalculateViewHeight(float width)
        {
            string lastCategory = null;
            float height = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Category != lastCategory)
                {
                    height += 30f;
                    lastCategory = entries[i].Category;
                }

                height += 34f;
            }

            return height + 8f;
        }

        //函数职责：绘制所有分类标题和条目按钮。
        private void DrawEntries(Rect viewRect)
        {
            string lastCategory = null;
            float curY = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                NingshaDevPlacementEntry entry = entries[i];
                if (entry.Category != lastCategory)
                {
                    Text.Font = GameFont.Medium;
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 28f), entry.Category);
                    Text.Font = GameFont.Small;
                    curY += 30f;
                    lastCategory = entry.Category;
                }

                Rect buttonRect = new Rect(0f, curY, viewRect.width, 30f);
                if (Widgets.ButtonText(buttonRect, entry.Label + "  [" + entry.DefName + "]"))
                {
                    SelectEntry(entry);
                }

                curY += 34f;
            }
        }

        //函数职责：根据条目创建并选中地图摆放 Designator。
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
