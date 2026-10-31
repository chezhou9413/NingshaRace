using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Race.Rendering
{
    //读书时让书本与头部使用同一帧的渲染朝向。
    public class PawnRenderNodeWorker_NingshaCarried : PawnRenderNodeWorker_Carried
    {
        //普通携带物沿用原版；展开的书本不再单独读取角色实时朝向。
        public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
        {
            Pawn pawn = parms.pawn;
            if (pawn.CurJobDef != JobDefOf.Reading || pawn.carryTracker?.CarriedThing is not Book book || !book.IsOpen)
            {
                base.PostDraw(node, parms, mesh, matrix);
                return;
            }

            Rot4 facing = parms.flipHead ? parms.facing.Opposite : parms.facing;
            Vector3 drawLoc = parms.matrix.Position() + OffsetFor(node, parms, out _);
            Vector3 offset = new Vector3(0f, 0f, -0.1f);
            if (facing == Rot4.North) offset.z = 0f;
            else if (facing == Rot4.East) offset.x = 0.18f;
            else if (facing == Rot4.West) offset.x = -0.18f;
            if (pawn.DevelopmentalStage == DevelopmentalStage.Child) offset.z -= 0.1f;
            offset.y = PawnRenderUtility.AltitudeForLayer(100f) * (facing == Rot4.North ? -1f : 1f);
            book.BookComp.Props.openGraphic.Graphic.Draw(drawLoc + offset, facing, book);
        }
    }
}
