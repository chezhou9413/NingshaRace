using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Components;
using RimWorld;
using Verse;
using Verse.Sound;

namespace NingshaRaceLib.DesertPit.AntColony.Buildings
{
    //固定射浆蚁沿用吐酸蚁的弹丸、射程、蓄力和冷却。
    public sealed class Building_AntAcidTurret : Building_AntColonyStructure
    {
        private int nextShotTick;
        private int warmupUntil;
        private Thing target;

        //低频获取共享目标；蓄力结束时再次检查射线。
        protected override void Tick()
        {
            base.Tick();
            int now = Find.TickManager.TicksGame;
            if (ColonyId <= 0 || now < nextShotTick) return;
            MapComponent_DesertPitAntColonies manager = Map.GetComponent<MapComponent_DesertPitAntColonies>();
            if (!manager.StructureCanOperate(ColonyId)) return;
            VerbProperties attack = DefOfRefs.NingshaRace_DesertPitAcidAntKind.race.Verbs.First(verb => verb.defaultProjectile != null);
            if (target == null)
            {
                if (!this.IsHashIntervalTick(60)) return;
                target = manager.TurretTarget(ColonyId, this, attack.range, attack.minRange);
                if (target != null) warmupUntil = now + (int)(attack.warmupTime * 60f);
                return;
            }
            if (!target.Spawned || target.Destroyed || Position.DistanceTo(target.Position) > attack.range
                || Position.DistanceTo(target.Position) < attack.minRange
                || target is Pawn pawn && (pawn.Dead || pawn.Downed)
                || !GenSight.LineOfSight(Position, target.Position, Map, true)) { target = null; return; }
            if (now < warmupUntil) return;
            Projectile projectile = (Projectile)GenSpawn.Spawn(attack.defaultProjectile, Position, Map);
            //使用原版距离命中率和偏射计算，固定射浆蚁没有角色射击技能加成。
            float accuracy = attack.GetHitChanceFactor(null, Position.DistanceTo(target.Position));
            ShootLine line = new ShootLine(Position, target.Position);
            if (Rand.Chance(accuracy))
                projectile.Launch(this, target, target, ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.NonTargetPawns);
            else
            {
                line.ChangeDestToMissWild(accuracy, false, Map);
                projectile.Launch(this, line.Dest, target, ProjectileHitFlags.NonTargetPawns | ProjectileHitFlags.NonTargetWorld);
            }
            attack.soundCast?.PlayOneShot(new TargetInfo(Position, Map));
            nextShotTick = now + (int)(attack.defaultCooldownTime * 60f);
            target = null;
        }

        //读档继续当前蓄力，不额外发射一次。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextShotTick, "nextShotTick");
            Scribe_Values.Look(ref warmupUntil, "warmupUntil");
            Scribe_References.Look(ref target, "target");
        }
    }
}
