using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.UI.Layout
{
    //类职责：以纵向流和等分列组合界面，统一字体测量和主体页脚分区。
    public sealed class NingshaLayout
    {
        private readonly Rect bounds;
        private float y;

        //构造职责：把局部布局游标放到内容区域顶端。
        public NingshaLayout(Rect bounds)
        {
            this.bounds = bounds;
            y = bounds.y;
        }

        //属性职责：返回尚未被顶部内容占用的区域。
        public Rect Remaining => new Rect(bounds.x, y, bounds.width, Mathf.Max(0f, bounds.yMax - y));

        //函数职责：按指定高度取出一行并推进游标，保持统一行间距。
        public Rect Take(float height, float gap = NingshaPalette.Gap)
        {
            Rect row = new Rect(bounds.x, y, bounds.width, height);
            y += height + gap;
            return row;
        }

        //函数职责：根据实际字体和可用宽度测量多行文字所需高度。
        public static float TextHeight(string text, float width, GameFont font = GameFont.Small)
        {
            using (new NingshaGuiScope(font))
            {
                Text.WordWrap = true;
                return Mathf.Max(Text.LineHeightOf(font), Mathf.Ceil(Text.CalcHeight(text ?? "", Mathf.Max(1f, width)))) + 4f;
            }
        }

        //函数职责：给标题、按钮或输入框提供适应字体回退的单行高度。
        public static float RowHeight(GameFont font = GameFont.Small, float padding = 10f)
        {
            return Text.LineHeightOf(font) + padding;
        }

        //函数职责：在同一行内按相等宽度划分控件并保留间隔。
        public static Rect Column(Rect row, int index, int count, float gap = NingshaPalette.Gap)
        {
            float width = Mathf.Max(0f, (row.width - gap * (count - 1)) / count);
            return new Rect(row.x + (width + gap) * index, row.y, width, row.height);
        }

        //函数职责：从窗口底部预留固定页脚，确保滚动主体不覆盖操作按钮。
        public static Rect BodyWithFooter(Rect area, float footerHeight, out Rect footer)
        {
            footer = new Rect(area.x, area.yMax - footerHeight, area.width, footerHeight);
            return new Rect(area.x, area.y, area.width, Mathf.Max(0f, footer.y - area.y - NingshaPalette.Gap));
        }
    }
}
