using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Combat;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.AntColony.Effects;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //破军蚁先显示锁定路线，再直线冲锋；沿途同巢成员同样承受撞击。
    public sealed class JobDriver_AntCharge : JobDriver
    {
        private List<IntVec3> route;
        private List<Thing> hit = new List<Thing>();
        private IntVec3 origin;
        private int windupStarted;
        private int chargeStarted;
        private bool charging;
        private int step;
        private float stepProgress;
        private int recoveryUntil;
        private Vector3 impactDirection;
        private Vector3 drawOffset;
        private DefModExtension_AntColony Settings => DefOfRefs.NingshaRace_DesertPitAntNest.GetModExtension<DefModExtension_AntColony>();
        private float WindupProgress => Mathf.Clamp01((Find.TickManager.TicksGame - windupStarted) / (float)Settings.chargeWindup);

        //在相邻格之间平滑绘制，不把高速冲锋显示成逐格闪现。
        public override Vector3 ForcedBodyOffset => drawOffset;

        //战斗不独占敌人。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        //蓄力锁定位置，实际起冲才进入冷却，障碍变化会中止冲锋。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !pawn.Spawned || pawn.Dead || pawn.Downed);
            Toil prepare = Toils_General.Do(delegate
            {
                if (!TargetB.IsValid)
                {
                    if (!AntChargeUtility.TryGetDestination(pawn, TargetA.Thing, Settings.chargeRange, out IntVec3 end))
                    { EndJobWith(JobCondition.Incompletable); return; }
                    job.targetB = end;
                }
                SetRoute();
                rotateToFace = TargetIndex.B;
                windupStarted = Find.TickManager.TicksGame;
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceCell(TargetB.Cell);
            });
            prepare.atomicWithPrevious = true;
            yield return prepare;
            Toil windup = Toils_General.Wait(Settings.chargeWindup).WithProgressBarToilDelay(TargetIndex.B);
            windup.atomicWithPrevious = true;
            windup.tickAction = delegate
            {
                if (pawn.Position != origin) SetRoute();
                pawn.rotationTracker.FaceCell(TargetB.Cell);
                Vector3 direction = (TargetB.Cell - origin).ToVector3().normalized;
                drawOffset = -direction * Mathf.SmoothStep(0f, 0.18f, WindupProgress);
                if (pawn.IsHashIntervalTick(30)) AntChargeEffects.Windup(pawn, WindupProgress);
            };
            yield return windup;

            Toil charge = ToilMaker.MakeToil("破军冲锋");
            charge.atomicWithPrevious = true;
            charge.defaultCompleteMode = ToilCompleteMode.Never;
            charge.initAction = delegate
            {
                if (pawn.Position != origin) SetRoute();
                charging = true;
                chargeStarted = Find.TickManager.TicksGame;
                drawOffset = Vector3.zero;
                pawn.GetComp<Comp_DesertPitAntMember>().NextAbilityTick = Find.TickManager.TicksGame + Settings.chargeCooldown;
                AntChargeEffects.Trail(pawn, origin, launch: true);
            };
            charge.tickAction = ChargeTick;
            yield return charge;
        }

        //被推开后仍冲向已经锁定的落点，不追踪敌人或重置蓄力。
        private void SetRoute()
        {
            origin = pawn.Position;
            route = GenSight.BresenhamCellsBetween(origin, TargetB.Cell).Where(cell => cell != origin).ToList();
            step = 0;
            stepProgress = 0f;
            drawOffset = Vector3.zero;
        }

        //逐刻积分加速后的位移，保留每格单次碰撞和斜向距离。
        private void ChargeTick()
        {
            drawOffset = Vector3.zero;
            if (recoveryUntil > 0)
            {
                if (Find.TickManager.TicksGame >= recoveryUntil) { EndJobWith(JobCondition.Succeeded); return; }
                float recoil = Mathf.Clamp01((Find.TickManager.TicksGame - recoveryUntil + Settings.chargeWallStunTicks) / 15f);
                drawOffset = impactDirection * (0.3f * (1f - recoil) - Mathf.Sin(recoil * Mathf.PI) * 0.12f);
                return;
            }
            if (step >= route.Count) { EndJobWith(JobCondition.Succeeded); return; }
            IntVec3 previous = step == 0 ? origin : route[step - 1];
            if (pawn.Position != previous)
            {
                SetRoute();
                if (route.Count == 0) { EndJobWith(JobCondition.Succeeded); return; }
                previous = origin;
            }
            IntVec3 next = route[step];
            bool blocked = AntChargeUtility.TryGetObstacle(Map, previous, next, out IntVec3 obstacleCell, out Building obstacle);
            float acceleration = Mathf.Clamp01((Find.TickManager.TicksGame - chargeStarted + 1f) / Settings.chargeAccelerationTicks);
            float speedFactor = Mathf.SmoothStep(0f, Settings.chargeSpeedFactor, acceleration);
            float cost = next.x != previous.x && next.z != previous.z ? pawn.TicksPerMoveDiagonal : pawn.TicksPerMoveCardinal;
            stepProgress += Mathf.Min(1f, speedFactor / cost);
            if (blocked)
            {
                drawOffset = (next - previous).ToVector3() * Mathf.Min(stepProgress, 0.3f);
                if (stepProgress >= 0.3f) Crash(obstacleCell, obstacle, (next - previous).ToVector3().normalized);
                return;
            }
            if (stepProgress < 1f)
            {
                drawOffset = (next - previous).ToVector3() * stepProgress;
                return;
            }
            stepProgress -= 1f;
            IntVec3 direction = new IntVec3(System.Math.Sign(next.x - previous.x), 0, System.Math.Sign(next.z - previous.z));
            HitCell(next, direction);
            if (!pawn.Spawned || pawn.Dead || pawn.jobs.curDriver != this) return;
            AntChargeEffects.Trail(pawn, previous);
            pawn.rotationTracker.FaceCell(next);
            pawn.Position = next;
            pawn.Notify_Teleported(false, false);
            step++;
        }

        //先进入恢复阶段，再结算一次墙体伤害，避免受击回调造成重复撞击。
        private void Crash(IntVec3 cell, Building obstacle, Vector3 direction)
        {
            recoveryUntil = Find.TickManager.TicksGame + Settings.chargeWallStunTicks;
            impactDirection = direction;
            pawn.pather.StopDead();
            pawn.stances.stunner.StunFor(Settings.chargeWallStunTicks, obstacle, addBattleLog: false, disableRotation: true);
            AntChargeEffects.WallImpact(Map, cell, pawn.thingIDNumber);
            if (obstacle != null && !hit.Contains(obstacle))
            {
                hit.Add(obstacle);
                obstacle.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Settings.chargeDamage, instigator: pawn));
            }
        }

        //每个实体只受击一次；建筑只能被撞击，角色与尸体可以逐格击退。
        private void HitCell(IntVec3 cell, IntVec3 direction)
        {
            foreach (Thing victim in cell.GetThingList(Map).ToArray())
            {
                if (victim == pawn || hit.Contains(victim)
                    || !(victim is Pawn) && !(victim is Corpse) && !(victim is Building && victim == TargetA.Thing)) continue;
                hit.Add(victim);
                AntChargeEffects.Impact(Map, cell, victim.thingIDNumber);
                victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Settings.chargeDamage, instigator: pawn));
                if (!pawn.Spawned || pawn.Dead || pawn.jobs.curDriver != this) return;
                if (victim is Building) continue;
                Thing movable = victim is Pawn dead && dead.Dead ? dead.Corpse : victim;
                if (movable == null || !movable.Spawned) continue;
                if (movable != victim) hit.Add(movable);
                IntVec3 destination = movable.Position;
                for (int i = 0; i < Settings.chargeKnockback; i++)
                {
                    IntVec3 next = destination + direction;
                    if (!AntChargeUtility.CornersClear(Map, destination, next) || !AntChargeUtility.CanEnter(Map, next)) break;
                    destination = next;
                }
                if (destination == movable.Position) continue;
                if (movable is Pawn other) { other.Position = destination; other.Notify_Teleported(); }
                else { movable.DeSpawn(); GenSpawn.Spawn(movable, destination, Map); }
            }
        }

        //成员绘制回调使用同一条实际路线，不另建绘制计时器。
        public void DrawIndicator()
        {
            if (route == null || route.Count == 0 || !pawn.Spawned || pawn.Dead || pawn.Downed || recoveryUntil > 0) return;
            if (!charging) AntChargeEffects.DrawIndicator(pawn, route);
            else AntChargeEffects.DrawRush(pawn, origin, route[route.Count - 1]);
        }

        //检查面板区分前摇、冲锋和撞墙后的僵直。
        public override string GetReport() => recoveryUntil > 0 ? "撞墙后正在恢复。" : charging ? "正在加速冲锋。"
            : "正在蓄力冲锋（" + Mathf.Max(0f, (Settings.chargeWindup - Find.TickManager.TicksGame + windupStarted) / 60f).ToString("0.0") + " 秒）。";

        //保存路线、阶段、已命中集合和移动进度，读档不重复扣冷却或造成伤害。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref route, "route", LookMode.Value);
            Scribe_Collections.Look(ref hit, "hit", LookMode.Reference);
            Scribe_Values.Look(ref origin, "origin");
            Scribe_Values.Look(ref windupStarted, "windupStarted");
            Scribe_Values.Look(ref chargeStarted, "chargeStarted");
            Scribe_Values.Look(ref charging, "charging");
            Scribe_Values.Look(ref step, "step");
            Scribe_Values.Look(ref stepProgress, "stepProgress");
            Scribe_Values.Look(ref recoveryUntil, "recoveryUntil");
            Scribe_Values.Look(ref impactDirection, "impactDirection");
        }
    }
}
