using UnityEngine;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //时间归一到 0～1；位移以格计，角度允许跨越半圈而不反向插值。
    public sealed class MeleeAnimationKey
    {
        public float time, angle, lean, trail;
        public Vector2 position, body;
        //关键帧只包含数值。
        internal MeleeAnimationKey Copy() => (MeleeAnimationKey)MemberwiseClone();
    }

    //每帧采样结果供武器及身体节点只读使用。
    internal struct MeleeAnimationPose
    {
        internal float Angle, Lean, Trail;
        internal Vector2 Position, Body;
        //读取端点姿态。
        internal static MeleeAnimationPose From(MeleeAnimationKey key) => new MeleeAnimationPose
        { Angle = key.angle, Lean = key.lean, Trail = key.trail, Position = key.position, Body = key.body };
        //所有部件共用同一时间采样。
        internal static MeleeAnimationPose Lerp(MeleeAnimationKey a, MeleeAnimationKey b, float t) => new MeleeAnimationPose
        {
            Angle = Mathf.LerpUnclamped(a.angle, b.angle, t), Lean = Mathf.LerpUnclamped(a.lean, b.lean, t),
            Trail = Mathf.LerpUnclamped(a.trail, b.trail, t), Position = Vector2.LerpUnclamped(a.position, b.position, t),
            Body = Vector2.LerpUnclamped(a.body, b.body, t)
        };
    }
}
