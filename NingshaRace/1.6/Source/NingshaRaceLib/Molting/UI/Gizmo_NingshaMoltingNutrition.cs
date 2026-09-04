using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Molting.Components;

namespace NingshaRaceLib.Molting.UI
{
    //类职责：以只读 Gizmo 显示凝砂族蜕皮营养及主动蜕皮和伤势保命阈值。
    [StaticConstructorOnStartup]
    public sealed class Gizmo_NingshaMoltingNutrition : Gizmo
    {
        //字段职责：保存需要显示的 Pawn 蜕皮组件。
        public CompNingshaMolting molting;

        //字段职责：提供蜕皮营养条的暖砂色填充纹理。
        private static readonly Texture2D BarTexture =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.82f, 0.52f, 0.2f));

        //字段职责：提供蜕皮营养条的透明空白纹理。
        private static readonly Texture2D EmptyBarTexture =
            SolidColorMaterials.NewSolidColorTexture(Color.clear);

        //构造函数职责：把蜕皮营养条排序在侵蚀值面板之后和普通按钮之前。
        public Gizmo_NingshaMoltingNutrition()
        {
            Order = -105f;
        }

        //函数职责：返回蜕皮营养条在 Gizmo 区域使用的固定宽度。
        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(180f, maxWidth);
        }

        //函数职责：按实际字体行高绘制标题、营养条和阈值摘要，并恢复全部 IMGUI 状态。
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
                string detailText = "主动蜕皮100 · 伤势保命60";
                float neededHeight = smallLine + gap + barHeight + gap + tinyLine;
                Text.Font = GameFont.Tiny;
                bool compact = neededHeight > innerRect.height || Text.CalcSize(detailText).x > innerRect.width;

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Rect titleRect = new Rect(innerRect.x, innerRect.y, innerRect.width, smallLine);
                Widgets.Label(titleRect, "蜕皮营养");

                Rect barRect = new Rect(innerRect.x, titleRect.yMax + gap, innerRect.width, barHeight);
                Widgets.FillableBar(barRect, molting.NutritionRatio, BarTexture, EmptyBarTexture, doBorder: true);

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(barRect, molting.MoltingNutrition.ToString("0.##")
                    + " / " + molting.Props.nutritionCapacity.ToString("0.##"));

                if (!compact)
                {
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

        //函数职责：生成蜕皮营养条悬停时显示的两种消费方式和当前层数说明。
        private string BuildTooltip()
        {
            return "主动蜕皮需要100点营养并增加一层蜕皮者状态；伤势保命需要60点营养。"
                + "\n\n当前蜕皮层数：" + molting.MoltingCount + " / 20";
        }
    }
}
