using UnityEngine;
using Verse;
using NingshaRaceLib.Erosion.Editor;
using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Rendering
{
    //让蛇头跟随真实头部方向，围绕固定蛇尾连续摆动。
    public sealed class PawnRenderNodeWorker_ErosionSnake : PawnRenderNodeWorker
    {
        //预览边框直接采用最终绘制矩阵，和当前蛇头的实际位置一致。
        public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
        {
            base.PostDraw(node, parms, mesh, matrix);
            ErosionSnakeEditorSession editor = ErosionSnakeEditorSession.Active;
            if (editor != null && editor.Pawn == parms.pawn && editor.RenderingPreview)
                editor.Preview.Record(node, parms, matrix);
        }

        //蛇头仅属于凝砂侵蚀体，可见朝向与翻转后的真实头部保持一致。
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!ErosionPawnUtility.IsNingshaErosionBody(parms.pawn)) return false;
            if (parms.flipHead) parms.facing = parms.facing.Opposite;
            return base.CanDrawNow(node, parms);
        }

        //倒地翻头时，材质方向与头部保持一致。
        protected override Material GetMaterial(PawnRenderNode node, PawnDrawParms parms)
        {
            if (parms.flipHead) parms.facing = parms.facing.Opposite;
            return base.GetMaterial(node, parms);
        }

        //连接点以贴图左下角 UV 记录；西向网格和调试镜像同时影响横坐标。
        protected override Vector3 PivotFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector2 point = node.Props.drawData.PivotForRot(parms.facing);
            Vector3 pivot = new Vector3(point.x - 0.5f, 0, point.y - 0.5f);
            if ((parms.facing == Rot4.West) != node.FlipGraphic(parms)) pivot.x = -pivot.x;
            return pivot;
        }

        //补偿围绕连接点缩放造成的平移，静止姿态仍与原画画布完全对齐。
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            if (parms.flipHead) parms.facing = parms.facing.Opposite;
            Vector3 offset = base.OffsetFor(node, parms, out pivot);
            Vector3 scale = base.ScaleFor(node, parms);
            //原版 AltitudeForLayer 会截断到 -10～100，把超出的深度补回蛇头自身偏移。
            float layer = base.LayerFor(node, parms);
            offset.y += layer * PawnRenderNode.SubInterval - PawnRenderUtility.AltitudeForLayer(layer);
            return offset + Vector3.Scale(pivot, scale - Vector3.one);
        }

        //深度只由当前朝向的部件配置决定，不随摆动改变。
        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            if (parms.flipHead) parms.facing = parms.facing.Opposite;
            return base.LayerFor(node, parms);
        }

        //活体跟随游戏时钟，尸体与静态头像使用原画姿态。
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            if (parms.flipHead) parms.facing = parms.facing.Opposite;
            Quaternion rotation = base.RotationFor(node, parms);
            ErosionSnakeEditorSession editor = ErosionSnakeEditorSession.Active;
            bool preview = editor != null && editor.Pawn == parms.pawn && editor.RenderingPreview;
            if (!preview && (parms.pawn.Dead || parms.Portrait || parms.Cache || parms.Statue)) return rotation;
            var props = (PawnRenderNodeProperties_ErosionSnake)node.Props;
            ErosionSnakeSway sway = props.SwayFor(parms.facing);
            float phase = (sway?.phase ?? props.swayPhase) + (parms.pawn.thingIDNumber % 997) * 0.013f;
            float amplitude = sway?.angle ?? props.swayAngle * (parms.facing.IsHorizontal ? props.sideAngleFactor : 1);
            float seconds = preview ? editor.Clock : Find.TickManager.TicksGame / 60f;
            float angle = ErosionSnakeMotion.Angle(seconds, sway?.period ?? props.swayPeriod, phase, amplitude);
            if ((parms.facing == Rot4.West) != node.FlipGraphic(parms)) angle = -angle;
            return rotation * Quaternion.AngleAxis(angle, Vector3.up);
        }
    }
}
