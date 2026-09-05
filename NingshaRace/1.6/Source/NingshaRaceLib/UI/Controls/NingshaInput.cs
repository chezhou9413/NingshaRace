using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.UI.Controls
{
    //类职责：提供保留原版文本编辑行为、带砂岩边框和清空交互的输入组件。
    public static class NingshaInput
    {
        //函数职责：绘制稳定命名的单行输入框，保留光标、选择、复制和粘贴语义。
        public static string TextField(Rect rect, string value, string key)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                GUI.backgroundColor = NingshaPalette.Stone;
                GUI.contentColor = NingshaPalette.Ink;
                GUI.SetNextControlName(key);
                string next = Widgets.TextField(rect, value ?? "");
                NingshaFrame.Border(rect, GUI.GetNameOfFocusedControl() == key ? NingshaPalette.Sand : NingshaPalette.Brass);
                return next;
            }
        }

        //函数职责：组合检索输入和清空按钮，空值时展示不会覆盖已输入文字的提示。
        public static string Search(Rect rect, string value, string key, string hint = "检索名称或定义…")
        {
            Rect field = rect;
            field.xMax -= rect.height + 6f;
            string next = TextField(field, value, key);
            if (next.Length == 0 && GUI.GetNameOfFocusedControl() != key)
                NingshaText.Label(field.ContractedBy(6f, 2f), hint, NingshaPalette.Muted);
            if (NingshaButton.Draw(new Rect(field.xMax + 6f, rect.y, rect.height, rect.height), "×", key + ":clear", next.Length > 0, "清空检索"))
            {
                GUI.FocusControl(null);
                return "";
            }
            return next;
        }
    }
}
