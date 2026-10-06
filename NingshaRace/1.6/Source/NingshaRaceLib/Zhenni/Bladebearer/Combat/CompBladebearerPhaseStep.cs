using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //只在追击当前近战敌人时自动跨越距离，冷却按游戏时间保存。
    public sealed class CompBladebearerPhaseStep : ThingComp
    {
        private int nextCastTick, arrivalStarted = -1;
        private bool arrivalParticlesPending;
        internal readonly BladebearerPhaseSnapshot Snapshot = new BladebearerPhaseSnapshot();
        private Pawn Pawn => (Pawn)parent;
        internal CompProperties_BladebearerPhaseStep Props => (CompProperties_BladebearerPhaseStep)props;
        internal float Arrival => arrivalStarted < 0 ? 0 : Mathf.Clamp01(1f - (Find.TickManager.TicksGame - arrivalStarted) / (float)Props.arrivalTicks);

        //冷却与聚合进度在保存期间继续保持，瞬态残影不写入存档。
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref nextCastTick, "bladebearerNextPhaseStep");
            Scribe_Values.Look(ref arrivalStarted, "bladebearerArrivalStarted", -1);
        }

        //分散检查帧；正在攻击、被收容、倒地或眩晕时不会抢占现有行为。
        public override void CompTick()
        {
            int now = Find.TickManager.TicksGame;
            if (arrivalParticlesPending && Pawn.Spawned)
            {
                if (Snapshot.Fresh && Arrival > 0)
                {
                    Pawn.Map.GetComponent<MapComponent_BladebearerPhaseEffects>().Aggregate(this, arrivalStarted);
                    arrivalParticlesPending = false;
                }
                if (Arrival <= 0) arrivalParticlesPending = false;
            }
            if (now < nextCastTick || (now + Pawn.thingIDNumber) % 10 != 0 || !Pawn.Spawned || Pawn.Dead
                || Pawn.Downed || Pawn.stances.FullBodyBusy || Pawn.IsOnHoldingPlatform
                || Pawn.CurJob?.def != JobDefOf.AttackMelee) return;
            Thing target = Pawn.CurJob.targetA.Thing;
            if (target == null || !target.Spawned || target.Map != Pawn.Map || !target.HostileTo(Pawn)
                || target is Pawn victim && (victim.Dead || victim.Downed)) return;
            float distance = Pawn.Position.DistanceToSquared(target.Position);
            if (distance < Props.minRange * Props.minRange || distance > Props.maxRange * Props.maxRange) return;
            if (!TryDestination(target, out IntVec3 destination)) return;
            Vector3 departure = Pawn.DrawPos;
            var effects = Pawn.Map.GetComponent<MapComponent_BladebearerPhaseEffects>();
            effects.Dissolve(this, departure, Props.departureSeconds, false);
            Pawn.pather.StopDead();
            Pawn.Position = destination;
            Pawn.Notify_Teleported(endCurrentJob: false, resetTweenedPos: true);
            Pawn.rotationTracker.Face(target.DrawPos);
            Pawn.stances.SetStance(new Stance_Cooldown(Props.arrivalTicks, target, null) { neverAimWeapon = true });
            arrivalStarted = now; nextCastTick = now + Props.cooldownTicks;
            arrivalParticlesPending = true;
            Snapshot.Clear();
        }

        //选择可见、无人占用且能够近战接触的落点，不穿墙进入密闭房间。
        private bool TryDestination(Thing target, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            float best = float.MaxValue;
            foreach (IntVec3 offset in GenAdj.AdjacentCells)
            {
                IntVec3 cell = target.Position + offset;
                if (!cell.InBounds(Pawn.Map) || !cell.Standable(Pawn.Map) || !cell.WalkableBy(Pawn.Map, Pawn)
                    || cell.GetFirstPawn(Pawn.Map) != null || cell.GetFirstThing<Fire>(Pawn.Map) != null
                    || !ReachabilityImmediate.CanReachImmediate(cell, target, Pawn.Map, PathEndMode.Touch, Pawn)
                    || !GenSight.LineOfSight(Pawn.Position, cell, Pawn.Map)) continue;
                float score = cell.DistanceToSquared(Pawn.Position);
                if (score < best) { best = score; destination = cell; }
            }
            return destination.IsValid;
        }

        //原版完成无尸体死亡后，地图继续绘制最后姿态的消散与火屑。
        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            if (prevMap != null) prevMap.GetComponent<MapComponent_BladebearerPhaseEffects>()
                .Dissolve(this, Snapshot.Origin, Props.deathSeconds, true);
            Snapshot.Clear();
        }

        //正常离图不携带旧地图残影；死亡离图留待原版 Notify_Killed 捕获。
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            arrivalParticlesPending = false;
            if (!Pawn.Dead) Snapshot.Clear();
        }
    }
}
