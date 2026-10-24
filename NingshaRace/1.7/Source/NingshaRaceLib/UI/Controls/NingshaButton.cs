using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Motion;

namespace NingshaRaceLib.UI.Controls
{
    //类职责：绘制具有刻印底纹、悬停过渡、选择状态和禁用说明的交互按钮。
    public static class NingshaButton
    {
        //函数职责：绘制并处理单次点击，禁用状态仅展示原因，不触发业务动作。
        public static bool Draw(Rect rect, string label, string key, bool enabled = true, string tip = null,
            bool selected = false, bool destructive = false)
        {
            using (new NingshaGuiScope(GameFont.Small))
            {
                bool over = Mouse.IsOver(rect);
                float hover = NingshaUiMotion.Hover(key, over && enabled);
                NingshaFrame.Panel(rect, hover, !enabled);
                Color accent = destructive ? NingshaPalette.Danger : selected ? NingshaPalette.Jade : NingshaPalette.Sand;
                if (selected || hover > 0f)
                    Widgets.DrawBoxSolid(new Rect(rect.x + 6f, rect.yMax - 4f, (rect.width - 12f) * (selected ? 1f : hover), 2f), accent);
                NingshaText.Label(rect.ContractedBy(5f, 2f), label,
                    enabled ? NingshaPalette.Ink : NingshaPalette.Muted, anchor: TextAnchor.MiddleCenter, tooltip: tip.NullOrEmpty());
                if (!tip.NullOrEmpty()) TooltipHandler.TipRegion(rect, tip);
                if (!enabled) return false;
                MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
                if (!Widgets.ButtonInvisible(rect, false)) return false;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                return true;
            }
        }
    }
}
