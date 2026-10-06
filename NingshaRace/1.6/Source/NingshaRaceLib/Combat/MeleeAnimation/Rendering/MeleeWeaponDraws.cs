using System;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //保留原版装备尺寸与深度，特殊分层武器和普通材质共用配置的握持基准。
    internal static class MeleeWeaponDraws
    {
        internal struct Context
        {
            internal CompMeleeAnimation Owner;
            internal PawnDrawParms Parms;
            internal Matrix4x4 Blade;
            internal Action<Matrix4x4> DrawLayers;
        }
        internal static Context Current;

        //统一镜像、握点、动作和刀光，非接入单位保持原版路径。
        internal static void Draw(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Thing equipment)
        {
            CompMeleeAnimation owner = Current.Owner;
            if (owner == null || !owner.Accepts(equipment)) { Graphics.DrawMesh(mesh, matrix, material, layer); return; }
            MeleeAnimationDef def = owner.RenderDefinition;
            //沿用原版持械与瞄准角度；本次攻击的瞄准方向已由播放状态固定。
            matrix.SetColumn(1, new Vector4(0, 1, 0, 0));
            if ((mesh == MeshPool.plane10Flip) != def.mirrorBlade)
                matrix *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
            Vector3 localGrip = Current.Blade.MultiplyPoint3x4(MeleeAnimationTransform.Uv(def.grip));
            matrix = matrix * Matrix4x4.Translate(localGrip)
                * Matrix4x4.Rotate(Quaternion.AngleAxis(def.HoldAngle(Current.Parms.facing), Vector3.up))
                * Matrix4x4.Translate(-localGrip);
            Vector2 hand = def.Hand(Current.Parms.facing);
            Vector3 target = Current.Parms.matrix.MultiplyPoint3x4(new Vector3(hand.x, 0, hand.y));
            Vector3 grip = (matrix * Current.Blade).MultiplyPoint3x4(MeleeAnimationTransform.Uv(def.grip));
            matrix.m03 += target.x - grip.x; matrix.m23 += target.z - grip.z;
            Matrix4x4 basis = matrix;
            MeleeAnimationPlayback play = owner.Current;
            bool animate = play.Active && play.Facing == Current.Parms.facing;
            if (animate) matrix = MeleeAnimationTransform.Weapon(matrix, play, def, Current.Blade, play.MotionTime);
            if (Current.DrawLayers != null) Current.DrawLayers(matrix);
            else GenDraw.DrawMeshNowOrLater(MeshPool.plane10, matrix, material, Current.Parms.DrawNow);
            if (animate) owner.Trail.Draw(owner, Current.Parms, basis, Current.Blade, play);
        }
    }
}
