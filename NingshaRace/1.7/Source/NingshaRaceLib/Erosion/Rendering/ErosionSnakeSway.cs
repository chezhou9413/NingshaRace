using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Rendering
{
    //保存单一朝向的实际摆幅，不再叠加侧面倍率。
    public sealed class ErosionSnakeSway
    {
        public Rot4 facing;
        public float angle;
        public float period = 6;
        public float phase;

        //为编辑草稿复制独立参数。
        public ErosionSnakeSway Copy() => (ErosionSnakeSway)MemberwiseClone();

        //加载和导入时拒绝无效方向、周期与角度。
        public IEnumerable<string> Errors()
        {
            if (!facing.IsValid) yield return "朝向必须是 North、East、South 或 West。";
            if (!Finite(angle) || angle < 0 || angle > 30) yield return "摆幅必须在 0～30 度之间。";
            if (!Finite(period) || period <= 0) yield return "摆动周期必须是大于零的秒数。";
            if (!Finite(phase)) yield return "摆动相位必须是有限数字。";
        }

        //检查外部数值输入。
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
