using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Rendering;

namespace NingshaRaceLib.UI.Controls
{
    //类职责：绘制带砂岩肌理、双层铜边和刻印角饰的共用容器。
    public static class NingshaFrame
    {
        //函数职责：为窗口、卡片或命令绘制完整底板，悬停值仅改变铜边亮度。
        public static void Panel(Rect rect, float hover = 0f, bool inset = false)
        {
            if (Event.current.type != EventType.Repaint) return;
            using (new NingshaGuiScope(GameFont.Small))
            {
                GUI.color = inset ? new Color(0.64f, 0.64f, 0.64f) : Color.white;
                GUI.DrawTextureWithTexCoords(rect, NingshaUiAssets.Stone,
                    new Rect(0f, 0f, rect.width / 256f, rect.height / 256f));
                GUI.color = Color.white;
                NingshaPanelGrain.Draw(rect.ContractedBy(1f), inset);
                Color edge = Color.Lerp(NingshaPalette.Brass, NingshaPalette.Sand, hover);
                Border(rect, edge);
                Border(rect.ContractedBy(3f), new Color(edge.r, edge.g, edge.b, 0.28f));
                Corner(rect.x + 2f, rect.y + 2f, 1f, 1f, edge);
                Corner(rect.xMax - 2f, rect.y + 2f, -1f, 1f, edge);
                Corner(rect.x + 2f, rect.yMax - 2f, 1f, -1f, edge);
                Corner(rect.xMax - 2f, rect.yMax - 2f, -1f, -1f, edge);
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

        //函数职责：组合相互垂直的短刻线形成古石板角饰。
        private static void Corner(float x, float y, float dx, float dy, Color color)
        {
            Widgets.DrawBoxSolid(new Rect(dx > 0f ? x : x - 9f, y, 9f, 2f), color);
            Widgets.DrawBoxSolid(new Rect(x, dy > 0f ? y : y - 9f, 2f, 9f), color);
        }

        //函数职责：绘制章节分隔线和中心刻印，建立同一面板内的视觉层级。
        public static void Divider(Rect rect)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.center.y, rect.width, 1f), NingshaPalette.Brass);
            Widgets.DrawBoxSolid(new Rect(rect.center.x - 3f, rect.center.y - 2f, 6f, 5f), NingshaPalette.Sand);
        }
    }
}
