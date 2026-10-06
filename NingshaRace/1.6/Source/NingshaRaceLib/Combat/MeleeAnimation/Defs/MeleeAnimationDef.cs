using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //可复用的近战动作库；单位握点、刀刃布局与四向曲线全部来自配置。
    public sealed class MeleeAnimationDef : Def
    {
        public Vector2 grip = new Vector2(0.5f, 0.25f);
        public Vector2 bladeStart = new Vector2(0.5f, 0.5f);
        public Vector2 bladeEnd = new Vector2(0.5f, 0.95f);
        public Vector2 handSouth, handEast, handWest, handNorth;
        public float holdAngleSouth, holdAngleEast, holdAngleWest, holdAngleNorth;
        public Vector2 bodyPivot;
        public bool mirrorBlade;
        public List<MeleeAnimationMove> moves = new List<MeleeAnimationMove>();

        //四向原图并不对称，握点分别校准。
        internal Vector2 Hand(Rot4 facing)
        {
            if (facing == Rot4.East) return handEast;
            if (facing == Rot4.West) return handWest;
            return facing == Rot4.North ? handNorth : handSouth;
        }

        //在原版持械角上叠加局部校准，零度保留原版姿态。
        internal float HoldAngle(Rot4 facing)
        {
            if (facing == Rot4.East) return holdAngleEast;
            if (facing == Rot4.West) return holdAngleWest;
            return facing == Rot4.North ? holdAngleNorth : holdAngleSouth;
        }

        //启动时指出不完整方向、时间轴及参数错误。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            foreach (var field in typeof(MeleeAnimationDef).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                if (field.GetValue(this) is float angle && (float.IsNaN(angle) || float.IsInfinity(angle) || angle < -180 || angle > 180))
                    yield return field.Name + "：待机角度范围为 -180～180 度。";
                if (!(field.GetValue(this) is Vector2 v)) continue;
                bool uv = field.Name == "grip" || field.Name == "bladeStart" || field.Name == "bladeEnd";
                float min = uv ? 0 : -1, max = 1;
                if (float.IsNaN(v.x) || float.IsNaN(v.y) || v.x < min || v.y < min || v.x > max || v.y > max)
                    yield return field.Name + "：握点坐标超出范围。";
            }
            if ((bladeEnd - bladeStart).sqrMagnitude < 0.001f) yield return "bladeStart / bladeEnd：刃段长度过短。";
            foreach (string error in MeleeAnimationValidation.Errors(moves)) yield return error;
        }
    }
}
