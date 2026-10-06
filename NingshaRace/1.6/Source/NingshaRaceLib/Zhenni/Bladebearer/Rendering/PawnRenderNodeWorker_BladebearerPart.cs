using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //把部件变换交给渲染树，保持移动、姿态与绘制深度一致。
    public sealed class PawnRenderNodeWorker_BladebearerPart : PawnRenderNodeWorker
    {
        //可见性变化由组件重建请求，单独查看只影响当前编辑对象。
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            var n = (PawnRenderNode_BladebearerPart)node;
            return n.Part.enabled && n.Part.VisibleAt(parms.facing) && (n.Owner.SoloId == null || n.Owner.SoloId == n.Part.id)
                && base.CanDrawNow(node, parms);
        }

        //深度和颜色分别提交单 Pass 材质，兼容原版即时预览只执行 Pass 0 的行为。
        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            var n = (PawnRenderNode_BladebearerPart)node;
            requests.Add(new PawnGraphicDrawRequest(node, BladebearerResources.Mesh,
                n.Part.particle ? null : BladebearerResources.Material(n.Part, n.DepthOnly, parms.DrawNow)));
        }

        //不让原版 PreDraw 写入共享材质颜色；完整参数由下方统一处理。
        public override void PreDraw(PawnRenderNode node, Material mat, PawnDrawParms parms) { }

        //辉光使用原版最终矩阵和独立属性块，不重建部件坐标，也不持有会被原版清空的属性块。
        public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
        {
            base.PostDraw(node, parms, mesh, matrix);
            var n = (PawnRenderNode_BladebearerPart)node;
            if (n.DepthOnly) return;
            if (!parms.DrawNow) n.Owner.Phase?.Snapshot.Record(n, matrix, parms.tint);
            if (n.Part.particle)
            {
                if (parms.DrawNow && n.Owner.CapturePreview)
                    n.Owner.Projections[n.Part.id] = BladebearerProjection.Capture(matrix, Find.PawnCacheCamera);
                mesh = n.Owner.Particles.Draw(n, parms, matrix);
                if (mesh == null) return;
            }
            if (parms.DrawNow)
            {
                if (n.Owner.CapturePreview) BladebearerBloomPreview.Record(n, mesh, matrix, parms.tint);
            }
            else BladebearerBloomSources.Record(n, mesh, matrix, parms.tint);
        }

        //在实际绘制主线程传递参数，并记录预览投影供拖动手柄使用。
        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            var n = (PawnRenderNode_BladebearerPart)node;
            BladebearerShaderParameters.Apply(n.Part, n.Owner.AnimationTime, parms.tint, n.MatPropBlock, parms.DrawNow ? material : null);
            if (!parms.DrawNow) BladebearerShaderParameters.ApplyPhase(n.Owner.Phase?.Arrival ?? 0, n.MatPropBlock);
            if (parms.DrawNow && !n.DepthOnly && n.Owner.CapturePreview && node.tree.TryGetMatrix(node, parms, out Matrix4x4 matrix))
                n.Owner.Projections[n.Part.id] = BladebearerProjection.Capture(matrix, Find.PawnCacheCamera);
            return n.MatPropBlock;
        }

        //二维位置映射到 Pawn 平面的 X/Z，Y 深度由层级统一生成。
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            pivot = Vector3.zero;
            Vector2 p = ((PawnRenderNode_BladebearerPart)node).Part.position;
            return new Vector3(p.x, 0, p.y);
        }
        //层级越大越靠前，不用跨 Pawn 的全局材质队列分层。
        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            BladebearerPart part = ((PawnRenderNode_BladebearerPart)node).Part;
            return part.layer;
        }
        //部件围绕自身画布中心旋转。
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
            => Quaternion.AngleAxis(((PawnRenderNode_BladebearerPart)node).Part.angle, Vector3.up);
        //画布尺寸保持 PSD 原始像素比例。
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector2 s = ((PawnRenderNode_BladebearerPart)node).Part.size;
            return new Vector3(s.x, 1, s.y);
        }
    }
}
