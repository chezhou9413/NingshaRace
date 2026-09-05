using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;

namespace NingshaRaceLib.UI.Panels
{
    //类职责：组合真实生成阶段、砂槽进度与可展开说明，不负责推进或取消地图生成。
    public static class NingshaGenerationPanel
    {
        //函数职责：呈现当前阶段并允许切换说明区域，所有百分比直接取自地图生成进度。
        public static void Draw(Rect rect, string stage, float progress, ref bool expanded)
        {
            NingshaLayout layout = new NingshaLayout(rect);
            NingshaText.Label(layout.Take(NingshaLayout.RowHeight()), stage, NingshaPalette.Ink);
            NingshaProgress.Draw(layout.Take(NingshaLayout.RowHeight(GameFont.Tiny, 8f)), progress,
                Mathf.FloorToInt(Mathf.Clamp01(progress) * 100f) + "%", NingshaPalette.Sand);
            if (NingshaButton.Draw(layout.Take(NingshaLayout.RowHeight()), expanded ? "收起生成说明" : "展开生成说明", "generation:details", selected: expanded))
                expanded = !expanded;
            if (expanded)
            {
                string help = "正在准备地下的房间和通道，请稍候。完成后会自动继续前进。等待期间暂时无法关闭窗口、保存或取消。";
                Rect remaining = layout.Remaining;
                if (NingshaLayout.TextHeight(help, remaining.width) <= remaining.height)
                    NingshaText.Paragraph(remaining, help);
                else TooltipHandler.TipRegion(remaining, help);
            }
        }
    }
}
