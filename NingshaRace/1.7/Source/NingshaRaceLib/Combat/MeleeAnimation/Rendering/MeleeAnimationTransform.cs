using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //沿用原版持械基准，刀刃、火焰与轨迹围绕同一真实刀柄运动。
    internal static class MeleeAnimationTransform
    {
        //原版基准已经指向本次目标，关键帧旋转在镜像后的武器局部坐标中叠加。
        internal static Matrix4x4 Weapon(Matrix4x4 basis, MeleeAnimationPlayback playback,
            MeleeAnimationDef definition, Matrix4x4 blade, float motionTime)
        {
            float t = Mathf.Clamp01(motionTime / playback.Duration);
            MeleeAnimationFacing direction = playback.Direction;
            MeleeAnimationPose pose = direction.Evaluate(t);
            float envelope = Mathf.Sin(t * Mathf.PI);
            Vector3 grip = blade.MultiplyPoint3x4(Uv(definition.grip));
            Vector2 hand = definition.Hand(playback.Facing);
            Vector2 shift = pose.Position + direction.offset * envelope
                + BodyPosition(hand, pose, definition.bodyPivot) - hand;
            Matrix4x4 offset = Matrix4x4.Translate(new Vector3(shift.x, 0, shift.y));
            Matrix4x4 result = offset * basis * Matrix4x4.Translate(grip)
                * Matrix4x4.Rotate(Quaternion.AngleAxis(pose.Angle + direction.angle * envelope, Vector3.up))
                * Matrix4x4.Translate(-grip);
            float baseLayer = playback.Facing == Rot4.North ? -10 : 90;
            result.m13 += PawnRenderUtility.AltitudeForLayer(direction.layer) - PawnRenderUtility.AltitudeForLayer(baseLayer);
            return result;
        }

        //所有身体图层围绕共同的下半身支点倾斜，保持头盔与火焰的相对位置。
        internal static Vector2 BodyPosition(Vector2 position, MeleeAnimationPose pose, Vector2 bodyPivot)
        {
            Vector3 pivot = new Vector3(bodyPivot.x, 0, bodyPivot.y);
            Vector3 point = Quaternion.AngleAxis(pose.Lean, Vector3.up)
                * (new Vector3(position.x, 0, position.y) - pivot) + pivot;
            return new Vector2(point.x + pose.Body.x, point.z + pose.Body.y);
        }

        //贴图 UV 对应平面 X/Z，源图向上始终是局部正 Z。
        internal static Vector3 Uv(Vector2 uv) => new Vector3(uv.x - 0.5f, 0, uv.y - 0.5f);
    }
}
