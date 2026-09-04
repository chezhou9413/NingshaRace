using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.UI
{
    //类职责：以只读 Gizmo 显示沙傀当前剩余存在时间与自动消散规则。
    [StaticConstructorOnStartup]
    public sealed class Gizmo_SandGolemLifetime : Gizmo
    {
        //字段职责：保存需要显示的沙傀生命周期状态。
        public SandGolemRenderState state;

        //字段职责：提供沙傀寿命条的砂金色填充纹理。
        private static readonly Texture2D BarTexture =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.76f, 0.62f, 0.3f));

        //字段职责：提供沙傀寿命条的透明空白纹理。
        private static readonly Texture2D EmptyBarTexture =
            SolidColorMaterials.NewSolidColorTexture(Color.clear);

        //构造函数职责：把沙傀寿命条排序到普通操作按钮之前。
        public Gizmo_SandGolemLifetime()
        {
            Order = -110f;
        }

        //函数职责：返回寿命条在 Gizmo 区域使用的固定宽度。
        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(180f, maxWidth);
        }

        //函数职责：按实际字体行高绘制标题、寿命条和自动消散说明，并恢复全部 IMGUI 状态。
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            const float height = 75f;
            const float padding = 6f;
            const float gap = 3f;

            int currentTick = Find.TickManager.TicksGame;
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), height);
            Widgets.DrawWindowBackground(outerRect);
            TooltipHandler.TipRegion(outerRect, BuildTooltip(currentTick));

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
                string detailText = state.phase == SandGolemPhase.Gathering ? "汇聚完成后开始计时" : "到期后自动消散";
                float neededHeight = smallLine + gap + barHeight + gap + tinyLine;
                Text.Font = GameFont.Tiny;
                bool compact = neededHeight > innerRect.height || Text.CalcSize(detailText).x > innerRect.width;

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Rect titleRect = new Rect(innerRect.x, innerRect.y, innerRect.width, smallLine);
                Widgets.Label(titleRect, "沙傀存在时间");

                Rect barRect = new Rect(innerRect.x, titleRect.yMax + gap, innerRect.width, barHeight);
                Widgets.FillableBar(barRect, state.LifetimeRatioAt(currentTick), BarTexture, EmptyBarTexture, doBorder: true);

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                int remainingTicks = state.RemainingLifetimeTicksAt(currentTick);
                Widgets.Label(barRect, remainingTicks.ToStringTicksToPeriod(allowSeconds: false, shortForm: true));

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

        //函数职责：生成寿命条悬停时显示的总时长与当前剩余时间说明。
        private string BuildTooltip(int currentTick)
        {
            return "沙傀完成汇聚后可以稳定存在 "
                + SandGolemUtility.LifetimeTicks.ToStringTicksToPeriod(allowSeconds: false)
                + "。\n\n当前剩余 "
                + state.RemainingLifetimeTicksAt(currentTick).ToStringTicksToPeriod(allowSeconds: false)
                + "，耗尽后会原地消散。";
        }
    }
}
