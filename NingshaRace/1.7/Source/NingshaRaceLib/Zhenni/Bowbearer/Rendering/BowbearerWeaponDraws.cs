using System.Linq;
using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//接收原版最终持械矩阵，把同一握点应用到全部弓臂帧。
internal static class BowbearerWeaponDraws
{
    internal sealed record Context(PawnRenderNode_BladebearerWeapon Node, PawnDrawParms Parms, CompBowbearerShooting Owner);
    internal static Context Active;

    //部件编号来自导出的固定四帧，不影响其他单位的图层。
    internal static int FrameOf(string id)
        => id != null && (id.StartsWith("BowUpper") || id.StartsWith("BowLower"))
            && id.Length == 9 && id[8] >= '0' && id[8] <= '3' ? id[8] - '0' : -1;

    //预览瞄准角与实际四向一致。
    internal static float PreviewAim(Rot4 facing) => facing.AsInt * 90f;

    //最终装备矩阵负责朝向，局部调整负责握点和短暂回位。
    internal static void Draw(Mesh sourceMesh, Matrix4x4 basis, Context context)
    {
        var owner = context.Owner;
        var settings = owner.Settings;
        var parms = context.Parms;
        var node = context.Node;
        var gripPart = node.Parts.First(p => !p.DepthOnly && p.Part.id == node.Owner.WeaponPartId).Part;
        Vector2 drawSize = owner.Pawn.equipment.Primary.Graphic.drawSize;
        basis.SetColumn(1, new Vector4(0, 1, 0, 0));
        //原稿弓腹朝左，先反转横向再叠加原版的左右持械镜像，保留上下弓臂关系。
        if (sourceMesh != MeshPool.plane10Flip) basis *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
        Vector3 localGrip = PartMatrix(gripPart, drawSize).MultiplyPoint3x4(new Vector3(settings.grip.x - .5f, 0, settings.grip.y - .5f));
        basis = basis * Matrix4x4.Translate(localGrip)
            * Matrix4x4.Rotate(Quaternion.AngleAxis(settings.Angle(parms.facing) + owner.Recoil(), Vector3.up))
            * Matrix4x4.Translate(-localGrip);
        Vector2 hand = settings.Hand(parms.facing);
        Vector3 target = parms.matrix.MultiplyPoint3x4(new Vector3(hand.x, 0, hand.y));
        Vector3 grip = basis.MultiplyPoint3x4(localGrip);
        basis.m03 += target.x - grip.x; basis.m23 += target.z - grip.z;
        int frame = owner.Frame;
        foreach (var partNode in node.Parts)
        {
            int partFrame = FrameOf(partNode.Part.id);
            if (partFrame >= 0 && partFrame != frame) continue;
            if (!partNode.Worker.CanDrawNow(partNode, parms)) continue;
            Matrix4x4 matrix = basis * PartMatrix(partNode.Part, drawSize);
            partNode.Worker.PostDraw(partNode, parms, BladebearerResources.Mesh, matrix);
            if (parms.DrawNow && node.Owner.CapturePreview && !partNode.DepthOnly)
                node.Owner.Projections[partNode.Part.id] = BladebearerProjection.Capture(matrix, Find.PawnCacheCamera);
        }
        if (owner.Progress >= 0 && node.Owner.SoloId == null)
        {
            //蓄力时箭停在弓口，成功放箭后由真实弹丸接替。
            float aim = owner.PreviewActive ? PreviewAim(parms.facing)
                : (((Stance_Warmup)owner.Pawn.stances.curStance).focusTarg.CenterVector3 - owner.Pawn.DrawPos).AngleFlat();
            Vector3 muzzle = Muzzle(owner.Pawn, parms.matrix.Position(), aim, parms.facing, settings);
            BowbearerArrowDraws.Draw(owner.VisualSettings, muzzle, Quaternion.AngleAxis(aim, Vector3.up), node.Owner.AnimationTime,
                parms.DrawNow, owner.NockedArrowProperties);
            BowbearerChargeDraw.Draw(owner, muzzle, parms);
        }
        if (parms.DrawNow && owner.PreviewActive) owner.PreviewDraw?.Invoke(parms.matrix.Position());
    }

    //按装备大小抵消原版缩放，保持身体和武器使用同一格单位。
    private static Matrix4x4 PartMatrix(BladebearerPart p, Vector2 drawSize)
        => Matrix4x4.TRS(new Vector3(p.position.x / drawSize.x, PawnRenderUtility.AltitudeForLayer(p.layer), p.position.y / drawSize.y),
            Quaternion.AngleAxis(p.angle, Vector3.up), new Vector3(p.size.x / drawSize.x, 1, p.size.y / drawSize.y));

    //弹丸从同一四向握持基准出发，放箭偏移沿真实弹道旋转。
    internal static Vector3 Muzzle(Pawn pawn, Vector3 origin, float angle, Rot4 facing, BowbearerSettings settings)
    {
        Vector2 hand = settings.Hand(facing);
        return origin + new Vector3(hand.x, 0, hand.y)
            + Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(settings.muzzle.x, 0, settings.muzzle.y);
    }
}

