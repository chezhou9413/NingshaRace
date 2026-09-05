using RimWorld;
using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Controls;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.UI.Gizmos
{
    //类职责：为凝砂能力呈现符印与砂槽冷却，继承原版施法、目标选择、分组和禁用检查。
    public sealed class Command_NingshaAbility : Command_Ability
    {
        private bool drawing;

        //构造职责：让原版能力命令完成说明和角色信息初始化，不在构造期间隐藏标题。
        public Command_NingshaAbility(Ability ability, Pawn pawn) : base(ability, pawn) { }

        //属性职责：仅在绘制时隐藏原版溢出标签，并交由共用石板接管背景。
        public override string LabelCap => drawing ? null : base.LabelCap;
        public override Texture2D BGTexture => BaseContent.ClearTex;
        public override Texture2D BGTextureShrunk => BaseContent.ClearTex;
        protected override bool DoTooltip => false;

        //函数职责：为能力符印保留与其他凝砂行动相同的宽度。
        public override float GetWidth(float maxWidth) => Mathf.Min(NingshaCommandLayout.Width, maxWidth);

        //函数职责：组合多选角色标题并保留原版施法和队列许可对交互事件的过滤。
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            using (new NingshaGuiScope(GameFont.Tiny))
            {
                defaultLabel = ability.def.LabelCap;
                if (parms.multipleSelected && Pawn.Name != null) defaultLabel += " (" + Pawn.Name.ToStringShort + ")";
                if (devGizmo) defaultLabel = "开发：" + defaultLabel;
                GizmoResult result = GizmoOnGUIInt(new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f), parms);
                bool blockedQueue = (ability.Casting || KeyBindingDefOf.QueueOrder.IsDownEvent) && !ability.CanQueueCast;
                if (result.State == GizmoState.Interacted && ability.CanCast && !blockedQueue) return result;
                return new GizmoResult(result.State);
            }
        }

        //函数职责：在原版禁用检查和悬停说明更新后绘制符印，并恢复绘制标记与全局状态。
        protected override GizmoResult GizmoOnGUIInt(Rect rect, GizmoRenderParms parms)
        {
            using (new NingshaGuiScope(GameFont.Tiny))
            {
                drawing = true;
                try { return base.GizmoOnGUIInt(rect, parms); }
                finally { drawing = false; }
            }
        }

        //函数职责：用实际冷却数值绘制砂槽和倒计时，不使用原版覆盖整块按钮的绿色填充。
        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            string remaining = ability.CooldownTicksRemaining > 0
                ? "再次使用还需 " + ability.CooldownTicksRemaining.ToStringTicksToPeriod() : null;
            NingshaCommandFace.Draw(this, rect, parms, abilityLayout: true, buttonMat: buttonMat, extraTip: remaining);
            if (ability.CooldownTicksRemaining <= 0) return;
            using (new NingshaGuiScope(GameFont.Tiny))
            {
                float ratio = Mathf.InverseLerp(ability.CooldownTicksTotal, 0f, ability.CooldownTicksRemaining);
                NingshaCommandLayout layout = new NingshaCommandLayout(rect, parms.shrunk, true);
                NingshaProgress.Draw(layout.Cooldown, ratio, null, NingshaPalette.Jade);
                if (!parms.shrunk && Mouse.IsOver(rect))
                {
                    //悬停时才展开时间标签，平时保留完整图标；角落快捷键和次数仍由原版绘制。
                    float line = Text.LineHeightOf(GameFont.Tiny) + 2f;
                    if (layout.Icon.height >= line && layout.Icon.yMax - line >= rect.y + Text.LineHeightOf(GameFont.Tiny) + 3f)
                    {
                        Rect counter = new Rect(layout.Icon.x, layout.Icon.yMax - line, layout.Icon.width, line);
                        Widgets.DrawBoxSolid(counter, NingshaPalette.Recess);
                        NingshaText.Label(counter, ability.CooldownTicksRemaining.ToStringTicksToPeriod(),
                            NingshaPalette.Sand, GameFont.Tiny, TextAnchor.MiddleCenter, tooltip: false);
                    }
                }
            }
        }
    }
}
