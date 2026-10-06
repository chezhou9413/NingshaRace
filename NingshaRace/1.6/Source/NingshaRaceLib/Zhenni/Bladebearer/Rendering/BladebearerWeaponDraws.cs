using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //把震尼实体的分层武器接到通用近战框架。
    internal static class BladebearerWeaponDraws
    {
        internal static PawnRenderNode_BladebearerWeapon ActiveNode;
        internal static PawnDrawParms ActiveParms;

        //轨迹采样使用配置指定的实体刃段，不固定为匕首。
        internal static Matrix4x4 BladeTransform()
        {
            BladebearerPart part = ActiveNode.Parts.First(n => !n.DepthOnly && n.Part.id == ActiveNode.Owner.WeaponPartId).Part;
            return PartTransform(part);
        }

        //框架已经完成握点、镜像与动画，所有刀上图层只叠加自己的美术变换。
        internal static void DrawLayers(Matrix4x4 matrix)
        {
            foreach (PawnRenderNode_BladebearerPart node in ActiveNode.Parts)
            {
                if (!node.Worker.CanDrawNow(node, ActiveParms)) continue;
                Matrix4x4 partMatrix = matrix * PartTransform(node.Part);
                if (!node.Part.particle)
                {
                    Material material = BladebearerResources.Material(node.Part, node.DepthOnly, ActiveParms.DrawNow);
                    BladebearerShaderParameters.Apply(node.Part, node.Owner.AnimationTime, ActiveParms.tint,
                        node.MatPropBlock, ActiveParms.DrawNow ? material : null);
                    if (!ActiveParms.DrawNow) BladebearerShaderParameters.ApplyPhase(node.Owner.Phase?.Arrival ?? 0, node.MatPropBlock);
                    GenDraw.DrawMeshNowOrLater(BladebearerResources.Mesh, partMatrix, material, ActiveParms.DrawNow, node.MatPropBlock);
                    if (ActiveParms.DrawNow && !node.DepthOnly && node.Owner.CapturePreview)
                        node.Owner.Projections[node.Part.id] = BladebearerProjection.Capture(partMatrix, Find.PawnCacheCamera);
                }
                node.Worker.PostDraw(node, ActiveParms, BladebearerResources.Mesh, partMatrix);
            }
        }

        //把美术画布换算成装备局部坐标，编辑器调整仍然生效。
        private static Matrix4x4 PartTransform(BladebearerPart part)
        {
            Vector2 size = ActiveNode.Owner.Pawn.equipment.Primary.Graphic.drawSize;
            return Matrix4x4.TRS(new Vector3(part.position.x / size.x,
                PawnRenderUtility.AltitudeForLayer(part.layer), part.position.y / size.y),
                Quaternion.AngleAxis(part.angle, Vector3.up), new Vector3(part.size.x / size.x, 1, part.size.y / size.y));
        }
    }
}
