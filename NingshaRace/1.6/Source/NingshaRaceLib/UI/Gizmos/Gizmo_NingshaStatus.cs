using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Layout;
using NingshaRaceLib.UI.Motion;
using NingshaRaceLib.UI.Windows;

namespace NingshaRaceLib.UI.Gizmos
{
    //类职责：将业务提供的状态数据组合为可点击展开的砂岩石板，不修改任何游戏数值。
    public abstract class Gizmo_NingshaStatus : Gizmo
    {
        //属性职责：由业务适配器提供标题、数值、摘要、规则及进度语义。
        protected abstract string Title { get; }
        protected abstract string Value { get; }
        protected abstract string Detail { get; }
        protected abstract string Help { get; }
        protected abstract float Fraction { get; }
        protected virtual Color Accent => NingshaPalette.Sand;
        protected virtual float Threshold => -1f;

        //函数职责：为状态石板预留固定宽度，同时服从 Gizmo 区域上限。
        public override float GetWidth(float maxWidth) => Mathf.Min(180f, maxWidth);

        //函数职责：以实际行高组合标题、砂槽和可选摘要，并把点击交给原版 Gizmo 分发器。
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
                bool hovered = Mouse.IsOver(rect);
                NingshaFrame.Panel(rect, NingshaUiMotion.Hover("status:" + Title + ":" + topLeft, hovered));
                NingshaLayout layout = new NingshaLayout(rect.ContractedBy(7f));
                float line = Text.LineHeightOf(GameFont.Tiny) + 2f;
                Rect header = layout.Take(Text.LineHeightOf(GameFont.Small) + 2f, 3f);
                NingshaText.Label(new Rect(header.x, header.y, header.width - 18f, header.height), Title, tooltip: false);
                NingshaText.Label(new Rect(header.xMax - 16f, header.y, 16f, header.height), "⋮", NingshaPalette.Brass);
                NingshaProgress.Draw(layout.Take(line + 2f, 3f), Fraction, Value, Accent, Threshold);
                if (layout.Remaining.height >= line)
                    NingshaText.Label(layout.Remaining, Detail, NingshaPalette.Muted, GameFont.Tiny, tooltip: false);
                TooltipHandler.TipRegion(rect, Title + " · " + Value + "\n" + Detail + "\n\n" + Help + "\n\n点击查看详情。");
                if (Widgets.ButtonInvisible(rect)) return new GizmoResult(GizmoState.Interacted, Event.current);
                return new GizmoResult(hovered ? GizmoState.Mouseover : GizmoState.Clear);
            }
        }

        //函数职责：打开当前状态和规则的只读详情，不把进度条点击解释为游戏操作。
        public override void ProcessInput(Event ev)
        {
            Find.WindowStack.Add(new Dialog_NingshaReadout(Title, Value + "\n" + Detail + "\n\n" + Help));
        }
    }
}
