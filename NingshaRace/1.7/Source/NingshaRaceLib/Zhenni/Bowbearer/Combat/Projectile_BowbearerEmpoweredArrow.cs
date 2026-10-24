using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//强化箭命中时只结算一次原版圆形爆炸，不叠加 Bullet 的单体伤害。
public sealed class Projectile_BowbearerEmpoweredArrow : Projectile_BowbearerArrow
{
    protected override bool Empowered => true;

    //与原版即时爆炸弹一致，碰撞或护盾拦截后在当前位置爆开。
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        ImpactVisual();
        Map map = Map;
        IntVec3 center = Position;
        Destroy();
        GenExplosion.DoExplosion(center, map, def.projectile.explosionRadius, DamageDef, launcher,
            DamageAmount, ArmorPenetration, def.projectile.soundExplode, equipmentDef, def, intendedTarget.Thing,
            damageFalloff: def.projectile.explosionDamageFalloff, doVisualEffects: false, screenShakeFactor: .15f);
    }
}
