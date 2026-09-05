using NingshaRaceLib.Molting.Components;
using NingshaRaceLib.UI.Gizmos;

namespace NingshaRaceLib.Molting.UI
{
    //类职责：将蜕皮营养、伤势保命阈值与层数映射为可展开的砂槽石板。
    public sealed class Gizmo_NingshaMoltingNutrition : Gizmo_NingshaStatus
    {
        public CompNingshaMolting molting;

        //构造职责：把蜕皮营养排列在侵蚀状态之后。
        public Gizmo_NingshaMoltingNutrition() { Order = -105f; }

        //属性职责：提供营养数值、层数摘要、真实填充比例与保命阈值刻线。
        protected override string Title => "蜕皮营养";
        protected override string Value => molting.MoltingNutrition.ToString("0.##") + " / " + molting.Props.nutritionCapacity.ToString("0.##");
        protected override string Detail => "蜕皮者 " + molting.MoltingCount + " / 20 层";
        protected override float Fraction => molting.NutritionRatio;
        protected override float Threshold => molting.Props.rescueNutritionCost / molting.Props.nutritionCapacity;
        protected override string Help => "主动蜕皮需要" + molting.Props.nutritionCapacity.ToString("0.##")
            + "点营养，并增加一层蜕皮者状态；伤势保命需要" + molting.Props.rescueNutritionCost.ToString("0.##") + "点营养。"
            + "\n\n绿色标记表示伤势严重时保命所需的营养。主动蜕皮请点击旁边的“蜕皮”按钮。"
            + "\n\n当前蜕皮层数：" + molting.MoltingCount + " / 20";
    }
}
