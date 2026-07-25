using RimWorld;
using Verse;

namespace NingshaRaceLib.Combat.BurialMountainGuard.Components
{
    //类职责：提供葬岳格挡模式的 XML 参数并绑定对应 Comp。
    public class CompProperties_BurialMountainGuardMode : CompProperties
    {
        public float damageReduction = 20f;
        public float chargeThreshold = 100f;
        public float releaseDamageMultiplier = 0.5f;
        public float releaseRadius = 3.9f;
        public DamageDef releaseDamageDef = DamageDefOf.Blunt;
        public float armorPenetration = 1f;
        public float shieldScale = 2.6f;
        public float burstScale = 7.8f;

        //构造函数职责：把当前配置绑定到葬岳格挡 Comp。
        public CompProperties_BurialMountainGuardMode()
        {
            compClass = typeof(Comp_BurialMountainGuardMode);
        }
    }
}
