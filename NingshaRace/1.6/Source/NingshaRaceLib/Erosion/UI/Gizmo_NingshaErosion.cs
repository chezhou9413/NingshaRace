using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Erosion.Components;

namespace NingshaRaceLib.Erosion.UI
{
    //类职责：以只读 Gizmo 显示凝砂族当前侵蚀值、最终上限和自然衰减状态。
    [StaticConstructorOnStartup]
    public sealed class Gizmo_NingshaErosion : Gizmo
    {
        //字段职责：保存需要显示的 Pawn 侵蚀组件。
        public CompNingshaErosion erosion;

        //字段职责：提供低侵蚀阶段的砂金色填充纹理。
        private static readonly Texture2D LowBarTexture =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.72f, 0.58f, 0.28f));

        //字段职责：提供中等侵蚀阶段的橙褐色填充纹理。
        private static readonly Texture2D MediumBarTexture =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.78f, 0.38f, 0.16f));

        //字段职责：提供高侵蚀阶段的侵蚀紫色填充纹理。
        private static readonly Texture2D HighBarTexture =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.48f, 0.16f, 0.52f));

        //字段职责：提供满值转化阶段的暗红色填充纹理。
        private static readonly Texture2D CriticalBarTexture =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.55f, 0.08f, 0.12f));

        //字段职责：提供侵蚀条的透明空白纹理。
        private static readonly Texture2D EmptyBarTexture =
            SolidColorMaterials.NewSolidColorTexture(Color.clear);

        //构造函数职责：把侵蚀条排序到普通能力按钮之前。
        public Gizmo_NingshaErosion()
        {
            Order = -110f;
        }

        //函数职责：返回侵蚀条在 Gizmo 区域使用的固定宽度。
        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(180f, maxWidth);
        }

        //函数职责：按实际字体行高绘制标题、数值条和衰减说明，并完整恢复 IMGUI 状态。
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            const float height = 75f;
            const float padding = 6f;
            const float gap = 3f;

            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), height);
            Widgets.DrawWindowBackground(outerRect);
            TooltipHandler.TipRegion(outerRect, BuildTooltip());

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.WordWrap = false;
                GUI.color = Color.white;
                Rect innerRect = outerRect.ContractedBy(padding);
                float smallLine = Text.LineHeightOf(GameFont.Small);
                float tinyLine = Text.LineHeightOf(GameFont.Tiny);
                float barHeight = Mathf.Max(18f, tinyLine);
                float neededHeight = smallLine + gap + barHeight + gap + tinyLine;
                string detailText = erosion.IsTransforming ? "侵蚀体转化中" : "每日自然下降 10 点";
                Text.Font = GameFont.Tiny;
                bool compact = neededHeight > innerRect.height
                    || Text.CalcSize(detailText).x > innerRect.width;

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Rect titleRect = new Rect(innerRect.x, innerRect.y, innerRect.width, smallLine);
                Widgets.Label(titleRect, "侵蚀值");

                float barY = titleRect.yMax + gap;
                Rect barRect = new Rect(innerRect.x, barY, innerRect.width, barHeight);
                Widgets.FillableBar(barRect, erosion.ErosionRatio, BarTexture(), EmptyBarTexture, doBorder: true);

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(barRect, erosion.CurrentErosion.ToString("F0") + " / " + erosion.MaxErosion.ToString("F0"));

                if (!compact)
                {
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Rect detailRect = new Rect(innerRect.x, barRect.yMax + gap, innerRect.width, tinyLine);
                    Widgets.Label(detailRect, detailText);
                }
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }

            return new GizmoResult(GizmoState.Clear);
        }

        //函数职责：按侵蚀比例返回当前阶段对应的填充纹理。
        private Texture2D BarTexture()
        {
            if (erosion.IsTransforming || erosion.ErosionRatio >= 1f)
            {
                return CriticalBarTexture;
            }
            if (erosion.ErosionRatio >= 0.75f)
            {
                return HighBarTexture;
            }
            if (erosion.ErosionRatio >= 0.5f)
            {
                return MediumBarTexture;
            }
            return LowBarTexture;
        }

        //函数职责：生成侵蚀条悬停时显示的完整规则说明。
        private string BuildTooltip()
        {
            string text = "侵蚀值会以每天 10 点的速度自然下降。侵蚀过载会增加 20 点，并清除凝砂之眼与召唤沙傀的冷却。";
            if (erosion.IsTransforming)
            {
                text += "\n\n当前已达到上限，侵蚀体转化将在 "
                    + erosion.TransformationTicksRemaining.ToStringTicksToPeriod()
                    + " 后完成。";
            }
            else
            {
                text += "\n\n达到最终上限会永久转化为敌对侵蚀体。";
            }
            return text;
        }
    }
}
