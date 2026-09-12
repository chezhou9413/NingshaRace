using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Rendering;

namespace NingshaRaceLib.UI.Controls
{
    //类职责：绘制砂岩与稀薄流沙底板，以单层细边界定共用容器。
    public static class NingshaFrame
    {
        //函数职责：为窗口、卡片或命令绘制砂岩与积沙底板，薄沙在其上流动，边框与前景内容保持固定。
        public static void Panel(Rect rect, float hover = 0f, bool inset = false)
        {
            if (Event.current.type != EventType.Repaint) return;
            using (new NingshaGuiScope(GameFont.Small))
            {
                GUI.color = inset ? new Color(0.64f, 0.64f, 0.64f) : Color.white;
                GUI.DrawTextureWithTexCoords(rect, NingshaUiAssets.Stone,
                    new Rect(0f, 0f, rect.width / 256f, rect.height / 256f));
                GUI.color = Color.white;
                //静态细沙与边缘积沙承载面板质感，透明流沙只提供轻微运动。
                NingshaPanelGrain.Draw(rect.ContractedBy(1f), inset);
                NingshaPanelDrift.Draw(rect.ContractedBy(1f), hover, inset);
                Color edge = Color.Lerp(NingshaPalette.Brass, NingshaPalette.Sand, hover);
                Border(rect, edge);
            }
        }

        //函数职责：以四条线描绘容器轮廓，不覆盖内部可交互内容。
        public static void Border(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 1f, rect.height), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        //函数职责：用单条细线分隔同一面板内的章节。
        public static void Divider(Rect rect)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.center.y, rect.width, 1f), NingshaPalette.Brass);
        }
    }
}
