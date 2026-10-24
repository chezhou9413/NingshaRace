using System;

namespace NingshaRaceLib.UI.Models
{
    //类职责：描述组合选择面板的一项操作及其说明，不依赖原版浮动菜单绘制。
    public sealed class NingshaChoice
    {
        public readonly string Label;
        public readonly string Description;
        public readonly Action Action;

        //构造职责：记录选项展示文本与业务动作，空动作表示不可选择。
        public NingshaChoice(string label, Action action, string description = null)
        {
            Label = label;
            Action = action;
            Description = description;
        }
    }
}
