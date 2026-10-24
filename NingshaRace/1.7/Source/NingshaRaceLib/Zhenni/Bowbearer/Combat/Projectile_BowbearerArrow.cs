using UnityEngine;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//幽箭保留原版命中与护盾判定，紫焰仅参与表现。
public class Projectile_BowbearerArrow : Bullet
{
    private BowbearerSpiralTrail trail;
    private BowbearerShootingDef shooting;
    private Vector3 visualOffset;
    private readonly MaterialPropertyBlock block = new();
    private Vector3 VisualPosition => ExactPosition + visualOffset * (1 - DistanceCoveredFraction);
    protected virtual bool Empowered => false;
    private BowbearerSettings Visuals => Empowered ? shooting.EmpoweredVisuals : shooting.settings;

    //原版起点负责命中计算，视觉起点从握持位置平滑接入同一个终点。
    public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget,
        ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
    {
        var pawn = (Pawn)launcher;
        var owner = pawn.GetComp<CompBowbearerShooting>();
        shooting = owner.Definition;
        base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        float angle = (destination - origin).AngleFlat();
        visualOffset = BowbearerWeaponDraws.Muzzle(pawn, origin, angle, pawn.Rotation, shooting.settings) - origin;
        EnsureTrail();
    }

    //箭矢配置随弹丸保存，短暂视觉历史不进入存档。
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref shooting, "bowbearerShooting");
        Scribe_Values.Look(ref visualOffset, "bowbearerVisualOffset");
    }

    //在弹丸正常更新后记录真实路径，终止帧由 Impact 或离图处理。
    protected override void TickInterval(int delta)
    {
        EnsureTrail();
        base.TickInterval(delta);
        if (Spawned && !landed) trail.Record(VisualPosition, ExactRotation, Find.TickManager.TicksGame / 60f);
    }

    //重新载入时从当前坐标建立新尾迹，不生成跨地图连线。
    private void EnsureTrail()
    {
        if (trail != null || shooting == null || !Spawned) return;
        trail = new BowbearerSpiralTrail(Visuals, thingIDNumber);
        trail.Record(VisualPosition, ExactRotation, Find.TickManager.TicksGame / 60f);
        Map.GetComponent<MapComponent_BowbearerTrails>().Add(trail);
    }

    //箭体显示与独立双螺旋使用同一游戏时钟。
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (shooting == null) return;
        BowbearerArrowDraws.Draw(Visuals, VisualPosition, ExactRotation, Find.TickManager.TicksGame / 60f, false, block);
    }

    //保存最后一段路径后交给原版伤害流程，不附加点燃或灼烧。
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        ImpactVisual();
        base.Impact(hitThing, blockedByShield);
    }

    //两种箭都先交接视觉历史，再由各自的单体或爆炸伤害流程销毁。
    protected void ImpactVisual()
    {
        EnsureTrail();
        trail?.Impact(VisualPosition, ExactRotation, Find.TickManager.TicksGame / 60f);
    }

    //弹丸销毁后地图继续管理短暂尾迹。
    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Spawned) trail?.Record(VisualPosition, ExactRotation, Find.TickManager.TicksGame / 60f);
        trail?.Stop();
        base.DeSpawn(mode);
        trail = null;
    }
}
