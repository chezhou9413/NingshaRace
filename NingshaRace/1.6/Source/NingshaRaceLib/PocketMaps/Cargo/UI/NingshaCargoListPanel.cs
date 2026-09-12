using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.PocketMaps.Cargo.UI
{
    //类职责：承载原版紧凑货运列表，复用搜索、排序及数量控件供拖动选择模组接入。
    internal sealed class NingshaCargoListPanel
    {
        private List<TransferableOneWay> source;
        private TransferableOneWayWidget animalsWidget;
        private TransferableOneWayWidget itemsWidget;
        private bool animals;

        //函数职责：仅在货运数据更换时建立两个列表，切换页签保留各自搜索、排序和滚动位置。
        public void Bind(List<TransferableOneWay> values, bool showAnimals)
        {
            animals = showAnimals;
            if (source == values) return;
            source = values;
            animalsWidget = CreateWidget(values.Where(item => item.ThingDef.category == ThingCategory.Pawn));
            itemsWidget = CreateWidget(values.Where(item => item.ThingDef.category != ThingCategory.Pawn));
        }

        //函数职责：在独立坐标组内绘制原版列表，防止固定位置的搜索与排序栏覆盖窗口标题。
        public void Draw(Rect area)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                Widgets.BeginGroup(area);
                try
                {
                    (animals ? animalsWidget : itemsWidget).OnGUI(area.AtZero());
                }
                finally
                {
                    Widgets.EndGroup();
                }
            }
        }

        //函数职责：建立每行三十像素的原版列表，为名称、库存和数量调整保留主要空间。
        private static TransferableOneWayWidget CreateWidget(IEnumerable<TransferableOneWay> items)
        {
            return new TransferableOneWayWidget(items, null, null, "TransferMapPortalColonyThingCountTip".Translate());
        }

        //函数职责：校验实际发送数量，数量编辑缓冲由原版数字输入控件管理。
        public static bool HasInvalidAmount(List<TransferableOneWay> items)
        {
            return items.Any(item => item.CountToTransfer < 0 || item.CountToTransfer > item.MaxCount);
        }
    }
}
