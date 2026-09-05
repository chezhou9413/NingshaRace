using System;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.UI.Foundation
{
    //结构职责：隔离凝砂控件的字体、对齐、换行、颜色和输入启用状态。
    public struct NingshaGuiScope : IDisposable
    {
        private readonly GameFont font;
        private readonly TextAnchor anchor;
        private readonly bool wrap;
        private readonly bool enabled;
        private readonly Color color;
        private readonly Color background;
        private readonly Color content;

        //构造职责：保存外部状态并建立可预测的凝砂文本绘制环境。
        public NingshaGuiScope(GameFont targetFont)
        {
            font = Text.Font;
            anchor = Text.Anchor;
            wrap = Text.WordWrap;
            enabled = GUI.enabled;
            color = GUI.color;
            background = GUI.backgroundColor;
            content = GUI.contentColor;
            Text.Font = targetFont;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
        }

        //函数职责：在正常返回、提前退出或异常展开时恢复调用方状态。
        public void Dispose()
        {
            Text.Font = font;
            Text.Anchor = anchor;
            Text.WordWrap = wrap;
            GUI.enabled = enabled;
            GUI.color = color;
            GUI.backgroundColor = background;
            GUI.contentColor = content;
        }
    }
}
