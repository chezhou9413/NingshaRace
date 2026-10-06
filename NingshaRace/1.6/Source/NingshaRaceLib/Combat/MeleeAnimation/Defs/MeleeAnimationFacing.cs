using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //方向有独立握持修正、深度和动作轨迹，身体贴图仍按自身四向选择。
    public sealed class MeleeAnimationFacing
    {
        public string facing;
        public Vector2 offset;
        public float angle, layer = 90;
        public List<MeleeAnimationKey> keys = new List<MeleeAnimationKey>();

        //复制可编辑关键帧。
        internal MeleeAnimationFacing Copy()
        {
            var copy = (MeleeAnimationFacing)MemberwiseClone();
            copy.keys = keys.Select(k => k.Copy()).ToList();
            return copy;
        }

        //单调三次插值穿过中间帧，不在每个关键帧强制减速至零。
        internal MeleeAnimationPose Evaluate(float time)
        {
            if (time <= 0) return MeleeAnimationPose.From(keys[0]);
            if (time >= 1) return MeleeAnimationPose.From(keys[keys.Count - 1]);
            for (int i = 1; i < keys.Count; i++)
            {
                if (time > keys[i].time) continue;
                float t = Mathf.InverseLerp(keys[i - 1].time, keys[i].time, time);
                return new MeleeAnimationPose
                {
                    Angle = Curve(i, t, k => k.angle), Lean = Curve(i, t, k => k.lean),
                    Trail = Curve(i, t, k => k.trail),
                    Position = new Vector2(Curve(i, t, k => k.position.x), Curve(i, t, k => k.position.y)),
                    Body = new Vector2(Curve(i, t, k => k.body.x), Curve(i, t, k => k.body.y))
                };
            }
            return default;
        }

        //保留重复姿态的蓄力或收势，转折处不超调，连续斩击处保持非零速度。
        private float Curve(int right, float t, System.Func<MeleeAnimationKey, float> value)
        {
            int left = right - 1;
            float h = keys[right].time - keys[left].time;
            float a = value(keys[left]), b = value(keys[right]);
            float d = (b - a) / h;
            float m0 = left == 0 ? 0 : Slope((a - value(keys[left - 1])) / (keys[left].time - keys[left - 1].time), d,
                keys[left].time - keys[left - 1].time, h);
            float m1 = right == keys.Count - 1 ? 0 : Slope(d, (value(keys[right + 1]) - b) / (keys[right + 1].time - keys[right].time),
                h, keys[right + 1].time - keys[right].time);
            float t2 = t * t, t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * a + (t3 - 2 * t2 + t) * h * m0
                + (-2 * t3 + 3 * t2) * b + (t3 - t2) * h * m1;
        }

        //不同号的速度代表真正转向；同向按相邻帧间隔求平滑切线。
        private static float Slope(float a, float b, float ha, float hb)
        {
            if (a * b <= 0) return 0;
            float w1 = 2 * hb + ha, w2 = hb + 2 * ha;
            return (w1 + w2) / (w1 / a + w2 / b);
        }
    }
}
