using RimWorld;
using UnityEngine;
using NingshaRaceLib.Erosion.Components;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Gizmos;

namespace NingshaRaceLib.Erosion.UI
{
    //类职责：把侵蚀状态适配到统一石板，保留数值、阶段颜色和转化倒计时。
    public sealed class Gizmo_NingshaErosion : Gizmo_NingshaStatus
    {
        public CompNingshaErosion erosion;

        //构造职责：将侵蚀状态放到普通能力之前。
        public Gizmo_NingshaErosion() { Order = -110f; }

        //属性职责：提供侵蚀石板的标题、即时数值、自然衰减摘要和真实比例。
        protected override string Title => "侵蚀值";
        protected override string Value => erosion.CurrentErosion.ToString("F0") + " / " + erosion.MaxErosion.ToString("F0");
        protected override string Detail => erosion.IsTransforming ? "侵蚀体转化中" : "每日自然下降 10 点";
        protected override float Fraction => erosion.ErosionRatio;
        protected override float Threshold => 0.75f;

        //属性职责：按危险等级选择砂金、赭红、侵蚀紫和转化警示色。
        protected override Color Accent => erosion.IsTransforming || Fraction >= 1f ? NingshaPalette.Danger
            : Fraction >= 0.75f ? NingshaPalette.Erosion : Fraction >= 0.5f ? NingshaPalette.Warning : NingshaPalette.Sand;

        //属性职责：提供可展开阅读的完整侵蚀规则与转化剩余时间。
        protected override string Help => "侵蚀值每天自然下降 10 点。侵蚀过载增加 20 点，并清除凝砂之眼与召唤沙傀的冷却。"
            + (erosion.IsTransforming ? "\n\n当前已达到上限，侵蚀体转化将在 "
                + erosion.TransformationTicksRemaining.ToStringTicksToPeriod() + " 后完成。"
                : "\n\n达到最终上限会永久转化为敌对侵蚀体。");
    }
}
