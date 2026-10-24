using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Race.Rendering
{
    //类职责：让凝砂身体伤口及绷带使用瘦长身体定位，并按真实身体轮廓裁剪。
    public sealed class NingshaWoundDrawer : PawnWoundDrawer
    {
        //函数职责：绑定伤口覆盖图所属角色。
        public NingshaWoundDrawer(Pawn pawn) : base(pawn) { }

        //函数职责：继承原版伤口选择、治疗状态和衣物遮挡，只调整身体覆盖图的几何与遮罩。
        protected override void WriteCache(CacheKey key, PawnDrawParms parms, List<DrawCall> writeTarget)
        {
            base.WriteCache(key, parms, writeTarget);
            if (key.layer != OverlayLayer.Body || key.bodyMesh == null) return;
            Graphic graphic = pawn.Drawer.renderer.BodyGraphic;
            Texture2D texture = graphic?.MatAt(key.pawnRot).mainTexture as Texture2D;
            if (texture == null) return;
            Vector3 bodySize = key.bodyMesh.bounds.size;
            for (int i = 0; i < writeTarget.Count; i++)
            {
                DrawCall call = writeTarget[i];
                //原版宽肩和短腿锚点在此种族窄长贴图上会落入透明区。
                call.matrix.m03 *= key.pawnRot.IsHorizontal ? 0.8f : 0.6f;
                if (call.matrix.m23 < -0.2f) call.matrix.m23 *= 1.45f;
                call.overlayMat = MaterialPool.MatFrom(new MaterialRequest
                {
                    mainTex = call.overlayMat.mainTexture,
                    maskTex = texture,
                    color = call.overlayMat.color,
                    shader = call.overlayMat.shader
                });
                Vector3 overlaySize = call.overlayMesh.bounds.size;
                Vector3 origin = new Vector3(call.matrix.m03, 0f, call.matrix.m23)
                    - call.overlayMesh.bounds.extents + key.bodyMesh.bounds.extents;
                bool flipped = graphic.EastFlipped && key.pawnRot == Rot4.East
                    || graphic.WestFlipped && key.pawnRot == Rot4.West;
                call.maskTexScale = new Vector4(overlaySize.x / bodySize.x, overlaySize.z / bodySize.z);
                call.maskTexOffset = new Vector4(origin.x / bodySize.x, origin.z / bodySize.z, flipped ? 1f : 0f);
                writeTarget[i] = call;
            }
        }
    }
}
