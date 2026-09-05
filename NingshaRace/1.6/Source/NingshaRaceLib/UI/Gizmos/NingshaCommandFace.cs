using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Motion;

namespace NingshaRaceLib.UI.Gizmos
{
    //类职责：为行动和开关命令组合石板底、图标、铭文和状态刻印，输入仍交给原版 Command。
    internal static class NingshaCommandFace
    {
        //函数职责：绘制命令面板，在标签带之外保留图标、快捷键和开关状态区域。
        public static void Draw(Command command, Rect rect, GizmoRenderParms parms, bool? active = null,
            bool abilityLayout = false, Material buttonMat = null, string extraTip = null)
        {
            using (new NingshaGuiScope(GameFont.Tiny))
            {
                bool enabled = !command.Disabled && !parms.lowLight;
                float hover = NingshaUiMotion.Hover("command:" + command.Label + ":" + rect.position, enabled && Mouse.IsOver(rect));
                NingshaFrame.Panel(rect, hover, !enabled);
                NingshaCommandLayout layout = new NingshaCommandLayout(rect, parms.shrunk, abilityLayout);
                if (command.icon != null)
                {
                    GUI.color = enabled ? command.IconDrawColor : command.IconDrawColor.SaturationChanged(0f).ToTransparent(0.5f);
                    NingshaCommandIcon.Draw(command, layout.Icon, buttonMat);
                    GUI.color = Color.white;
                }
                if (layout.HasLabel)
                {
                    Widgets.DrawBoxSolid(layout.Label, new Color(0.055f, 0.047f, 0.035f, 0.8f));
                    NingshaText.Label(layout.Label.ContractedBy(1f, 0f), command.Label, enabled ? NingshaPalette.Ink : NingshaPalette.Muted,
                        GameFont.Tiny, TextAnchor.MiddleCenter, tooltip: false);
                }
                TooltipHandler.TipRegion(rect, command.Label + "\n" + command.Desc
                    + (command.Disabled ? "\n\n" + command.disabledReason : "") + command.DescPostfix
                    + (extraTip.NullOrEmpty() ? "" : "\n\n" + extraTip));
                if (active.HasValue)
                {
                    Rect seal = new Rect(rect.xMax - 12f, rect.y + 6f, 6f, 6f);
                    Widgets.DrawBoxSolid(seal, active.Value ? NingshaPalette.Jade : NingshaPalette.Recess);
                    NingshaFrame.Border(seal.ExpandedBy(1f), NingshaPalette.Brass);
                }
            }
        }
    }
}
