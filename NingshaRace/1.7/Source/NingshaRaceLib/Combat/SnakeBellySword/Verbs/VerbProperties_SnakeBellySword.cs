using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Combat.SnakeBellySword.Rendering;
using NingshaRaceLib.Combat.SnakeBellySword.Tracking;
using NingshaRaceLib.Combat.SnakeBellySword.Utility;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Verbs
{
    //类职责：保存蛇腹剑扇形攻击使用的范围和击退参数。
    public class VerbProperties_SnakeBellySword : VerbProperties
    {
        //字段职责：扇形总角度。
        public float coneAngle = 90f;

        //字段职责：每次命中后允许的最大击退格数。
        public int knockbackCells = 2;

        //字段职责：攻击时隐藏武器并播放动画的持续 Tick 数。
        public int weaponHiddenTicks = 42;

        //字段职责：定义蛇腹剑逐帧动画包含的总帧数。
        public int animationFrameCount = 21;

        //字段职责：定义各段伤害在逐帧动画中的结算帧，最后一帧同时触发击退。
        public List<int> damageFrames;

        //字段职责：攻击 Mote 相对基础绘制尺寸的缩放倍率。
        public float effectScale = 1.5f;

        //字段职责：定义每段伤害相对基础伤害的最小随机倍率。
        public float damageFactorMin = 0.8f;

        //字段职责：定义每段伤害相对基础伤害的最大随机倍率。
        public float damageFactorMax = 1.2f;

        //字段职责：定义未显式配置穿甲时按最终伤害换算穿甲的倍率。
        public float automaticArmorPenetrationFactor = 0.015f;

        //函数职责：指定蛇腹剑使用的自定义攻击 Verb 类型。
        public VerbProperties_SnakeBellySword()
        {
            verbClass = typeof(Verb_SnakeBellySword);
        }
    }
}
