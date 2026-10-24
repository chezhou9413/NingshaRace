using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.UI.Gizmos
{
    //类职责：以绿松石刻印呈现开关状态，保留原版开关分组判定和切换语义。
    public class Command_NingshaToggle : Command_Toggle
    {
        private bool drawing;

        //属性职责：禁用原版视觉背景及溢出标签，开关状态改由统一面板呈现。
        public override Texture2D BGTexture => BaseContent.ClearTex;
        public override Texture2D BGTextureShrunk => BaseContent.ClearTex;
        public override string LabelCap => drawing ? null : base.LabelCap;
        protected override bool DoTooltip => false;

        //函数职责：让开关符印与行动符印使用相同宽度。
        public override float GetWidth(float maxWidth) => Mathf.Min(NingshaCommandLayout.Width, maxWidth);

        //函数职责：调用原版命令核心绘制，避免再绘制原版右上角复选框。
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            return GizmoOnGUIInt(new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f), parms);
        }

        //函数职责：绘制与实际开关状态一致的符印并保留隐藏禁用状态的约定。
        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            NingshaCommandFace.Draw(this, rect, parms, disabled && hideIconIfDisabled ? (bool?)null : isActive(), buttonMat: buttonMat);
        }

        //函数职责：隔离原版命令内部对全局字体和颜色的修改。
        protected override GizmoResult GizmoOnGUIInt(Rect rect, GizmoRenderParms parms)
        {
            using (new NingshaGuiScope(GameFont.Tiny))
            {
                drawing = true;
                try { return base.GizmoOnGUIInt(rect, parms); }
                finally { drawing = false; }
            }
        }
    }
}
