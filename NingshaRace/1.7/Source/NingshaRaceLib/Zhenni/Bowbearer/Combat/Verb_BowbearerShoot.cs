using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//保留原版发射、命中率和射线判定，只通知视觉播放状态。
public sealed class Verb_BowbearerShoot : Verb_Shoot
{
    private bool empoweredShot;
    private CompBowbearerShooting Owner => CasterPawn?.GetComp<CompBowbearerShooting>();
    //装备尚未绑定持有者时，原版属性查询仍能得到基础预热时间。
    public override float WarmupTime => base.WarmupTime * (Owner?.NextEmpowered == true ? Owner.Settings.empowered.warmupMultiplier : 1);
    public override ThingDef Projectile => empoweredShot ? Owner.Definition.EmpoweredProjectile : base.Projectile;

    //瞄准范围提示反映下一箭，已完成射击的日志仍使用锁定的弹丸类型。
    public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
    {
        needLOSToCenter = true;
        return Owner?.NextEmpowered == true ? Owner.Definition.EmpoweredProjectile.projectile.explosionRadius : 0;
    }

    //在原版计算预热和弹道前锁定本发类型，发射后的计数变化不会改写战斗日志。
    public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false,
        bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
    {
        if (state == VerbState.Bursting) return false;
        empoweredShot = Owner.NextEmpowered;
        return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
    }

    //原版返回发射成功后才播放释放动画。
    protected override bool TryCastShot()
    {
        bool fired = base.TryCastShot();
        if (fired) Owner.Released(empoweredShot);
        return fired;
    }

    //读档后正在蓄力的箭仍保留原有类型。
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref empoweredShot, "bowbearerEmpoweredShot");
    }
}
