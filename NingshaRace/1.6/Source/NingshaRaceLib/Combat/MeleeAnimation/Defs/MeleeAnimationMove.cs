using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //一套攻击的节奏、紫焰刀光及四向关键帧。
    public sealed class MeleeAnimationMove
    {
        public string id, label;
        public float weight = 1, duration = 0.75f, contact = 0.36f, hitstop = 0.055f;
        public int strikeCount = 1;
        public float strikeInterval = 0.18f;
        public float trailLifetime = 0.22f, trailWidth = 0.8f, trailIntensity = 1.1f;
        public float flameDistortion = 0.65f, bloom = 0.7f;
        public float effectSpeed = 1;
        public float chargeIntensity, chargeReady = 0.35f, chargeFlash = 1.3f, chargeScale = 1;
        public int sparks = 14;
        public float sparkLifetime = 0.55f, sparkSpeed = 0.32f, sparkSize = 0.012f;
        public Color color = new Color(0.59f, 0.12f, 1f, 0.9f);
        public List<MeleeAnimationFacing> directions = new List<MeleeAnimationFacing>();

        //连击间隔使用秒，伤害、方向修正与火屑换算到同一动作进度。
        internal float ContactAt(int strike) => contact + strike * strikeInterval / duration;
        internal float LastContact => ContactAt(strikeCount - 1);

        //返回明确配置的方向，错误交给 Def 和导入校验。
        internal MeleeAnimationFacing For(Rot4 facing) => directions.First(d => d.facing == facing.ToStringWord());
        //美术草稿独立于 Def 和其他 Pawn。
        internal MeleeAnimationMove Copy()
        {
            var copy = (MeleeAnimationMove)MemberwiseClone();
            copy.directions = directions.Select(d => d.Copy()).ToList();
            return copy;
        }
    }
}
