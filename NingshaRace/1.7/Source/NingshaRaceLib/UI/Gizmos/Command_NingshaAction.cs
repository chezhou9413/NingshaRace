using UnityEngine;
using Verse;
using NingshaRaceLib.UI.Foundation;

namespace NingshaRaceLib.UI.Gizmos
{
    //类职责：替换凝砂行动命令的视觉部分，保留原版快捷键、分组、教学提示和动作派发。
    public class Command_NingshaAction : Command_Action
    {
        private bool drawing;

        //属性职责：让原版背景和底部溢出标签留空，由组合面板在按钮内绘制。
        public override Texture2D BGTexture => BaseContent.ClearTex;
        public override Texture2D BGTextureShrunk => BaseContent.ClearTex;
        public override string LabelCap => drawing ? null : base.LabelCap;
        protected override bool DoTooltip => false;

        //函数职责：使用紧凑统一宽度，使图标与名称更贴合命令边框。
        public override float GetWidth(float maxWidth) => Mathf.Min(NingshaCommandLayout.Width, maxWidth);

        //函数职责：在原版图标绘制时机插入凝砂命令视觉，避免重写输入分发逻辑。
        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            NingshaCommandFace.Draw(this, rect, parms, buttonMat: buttonMat);
        }

        //函数职责：围绕原版绘制流程隔离全部全局界面状态。
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
