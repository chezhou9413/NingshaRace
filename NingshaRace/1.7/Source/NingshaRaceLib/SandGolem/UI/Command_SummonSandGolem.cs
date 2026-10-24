using NingshaRaceLib.SandGolem.Automation;
using NingshaRaceLib.UI.Foundation;
using NingshaRaceLib.UI.Gizmos;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.SandGolem.UI
{
    //类职责：为召唤沙傀图标提供独立循环开关和蓝色开启指示，保留原有施法按钮。
    public sealed class Command_SummonSandGolem : Command_NingshaAbility
    {
        //构造职责：复用凝砂能力图标、标签和冷却槽。
        public Command_SummonSandGolem(Ability ability, Pawn pawn) : base(ability, pawn) { }

        //函数职责：每名召唤者独立显示开关，避免多选时操作对象不明确。
        public override bool GroupsWith(Gizmo other) => false;

        //函数职责：先处理循环按钮点击，消费事件后再让原版处理施法按钮。
        protected override GizmoResult GizmoOnGUIInt(Rect rect, GizmoRenderParms parms)
        {
            Rect toggle = new Rect(rect.xMax - 24f, rect.y + 3f, 21f, 21f);
            if (Widgets.ButtonInvisible(toggle)) GameComponent_SandGolemAutoSummon.Current.Toggle(Pawn);
            GizmoResult result = base.GizmoOnGUIInt(rect, parms);
            using (new NingshaGuiScope(GameFont.Tiny))
            {
                GUI.color = Color.white;
                Widgets.DrawBoxSolid(toggle, new Color(0.08f, 0.09f, 0.1f, 0.85f));
                DrawLoop(toggle);
                bool enabled = GameComponent_SandGolemAutoSummon.Current.Enabled(Pawn);
                TooltipHandler.TipRegion(toggle, "沙傀自动召唤：" + (enabled ? "开启" : "关闭")
                    + "\n未征召时可开启；沙傀死亡或自然消散后，在当前任务结束后前往居住区沙地补召。征召时关闭。主动收回不补召。");
                if (enabled)
                {
                    NingshaCommandLayout layout = new NingshaCommandLayout(rect, parms.shrunk, true);
                    for (int i = 0; i < 5; i++)
                        Widgets.DrawBoxSolid(new Rect(layout.Icon.x, layout.Icon.yMax - i - 1f, layout.Icon.width, 1f),
                            new Color(0.25f, 0.65f, 1f, (5f - i) * 0.12f));
                }
            }
            return result;
        }

        //函数职责：使用白色线段绘制循环箭头，不依赖字体是否包含特殊符号。
        private static void DrawLoop(Rect rect)
        {
            Vector2 center = rect.center;
            Vector2 previous = center + new Vector2(6f, 0f);
            for (int i = 1; i <= 16; i++)
            {
                float angle = i * 300f / 16f * Mathf.Deg2Rad;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6f;
                Widgets.DrawLine(previous, next, Color.white, 1.5f);
                previous = next;
            }
            Widgets.DrawLine(previous, previous + new Vector2(-4f, 0f), Color.white, 1.5f);
            Widgets.DrawLine(previous, previous + new Vector2(0f, 4f), Color.white, 1.5f);
        }
    }
}
