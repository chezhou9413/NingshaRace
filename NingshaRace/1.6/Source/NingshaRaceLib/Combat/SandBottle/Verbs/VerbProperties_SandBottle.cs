using RimWorld;
using Verse;

using NingshaRaceLib.Combat.SandBottle.Utility;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Utility;

namespace NingshaRaceLib.Combat.SandBottle.Verbs
{
    //类职责：保存沙瓶扇形攻击、滞缓状态和 ChezhouLib 特效播放参数。
    public class VerbProperties_SandBottle : VerbProperties
    {
        //字段职责：定义沙瓶攻击的扇形总角度。
        public float coneAngle = 60f;

        //字段职责：定义每个有效目标承受的热能基础伤害。
        public float damageAmount = 20f;

        //字段职责：定义沙瓶命中目标时使用的伤害类型。
        public DamageDef damageDef = DamageDefOf.Burn;

        //字段职责：定义沙尘滞缓状态持续的游戏 Tick 数。
        public int slowDurationTicks = 300;

        //字段职责：定义 ChezhouLib 注册特效使用的模组标识。
        public string effectModId = "NingshaRace";

        //字段职责：定义 ChezhouLib 注册的粒子 Prefab 名称。
        public string effectName = "SandThrowBurst";

        //字段职责：定义粒子特效在地图平面上的整体缩放。
        public float effectScale = 0.75f;

        //字段职责：定义粒子特效垂直深度相对地图平面的压缩倍率。
        public float effectDepthScale = 0.05f;

        //字段职责：定义粒子实例被 ChezhouLib 对象池回收前的秒数。
        public float effectLifetime = 5.5f;

        //构造函数职责：指定沙瓶使用的自定义远程攻击 Verb。
        public VerbProperties_SandBottle()
        {
            verbClass = typeof(Verb_SandBottle);
        }
    }
}
