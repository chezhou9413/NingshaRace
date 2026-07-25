using RimWorld;
using Verse;

namespace NingshaRaceLib.Combat.FallingMountainSlash.Defs
{
    //类职责：保存坠岳斩飞行挥砍与落地冲击使用的可配置玩法参数。
    public class FallingMountainSlashDefExtension : DefModExtension
    {
        //字段职责：定义飞行进度达到何种比例时触发挥砍。
        public float attackProgress = 0.55f;

        //字段职责：定义落地范围伤害与锁定目标重击的有效半径。
        public float landingImpactRadius = 2.9f;

        //字段职责：定义落地冲击对范围内 Pawn 造成的基础伤害。
        public float landingAreaDamage = 20f;

        //字段职责：定义落地冲击对锁定目标额外造成的基础伤害。
        public float lockedTargetDamage = 40f;

        //字段职责：定义落地冲击使用的伤害类型。
        public DamageDef landingDamageDef = DamageDefOf.Cut;

        //字段职责：定义落地冲击使用的护甲穿透比例。
        public float landingArmorPenetration = 1f;

        //字段职责：定义玩家查看落点地图时的震屏强度。
        public float landingScreenShake = 0.45f;
    }
}
