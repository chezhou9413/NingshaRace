using System.Linq;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //一次出手持有完整攻击周期，原版单次反击任务结束也不会取消尚未落下的刀。
    public sealed class Stance_MeleeAnimation : Stance_Busy
    {
        private string moveId;
        private Rot4 facing;
        private int windupTicks, elapsedTicks, strikeCount, impactsResolved;
        private float duration, contact, aimAngle, stop, strikeInterval;
        private bool restoreVisuals, releasingImpact, ending;
        private bool targetStartedDowned;
        private CompMeleeAnimation Owner => Pawn.TryGetComp<CompMeleeAnimation>();
        internal Rot4 Facing => facing;
        internal bool Committed => !ending && ticksLeft > 0 && Pawn.Spawned && !Pawn.Dead && !Pawn.Downed
            && verb != null && Owner.Accepts(verb.EquipmentSource);
        public override bool StanceBusy => !releasingImpact;

        //读档由原版恢复姿态和 Verb 引用。
        public Stance_MeleeAnimation() { }

        //接触点决定伤害时刻，连击至少留足完整动作时间。
        internal Stance_MeleeAnimation(CompMeleeAnimation owner, Verb attack)
            : base(attack.verbProps.AdjustedCooldownTicks(attack, owner.Pawn), attack.CurrentTarget, attack)
        {
            moveId = owner.Playback.Move.id; facing = owner.Playback.Facing;
            duration = owner.Playback.Duration; contact = owner.Playback.Move.contact;
            aimAngle = owner.Playback.AimAngle; stop = owner.Playback.Stop;
            strikeCount = owner.Playback.Move.strikeCount; strikeInterval = owner.Playback.Move.strikeInterval;
            windupTicks = Mathf.Max(1, Mathf.RoundToInt(duration * contact * 60));
            ticksLeft = Mathf.Max(ticksLeft, Mathf.CeilToInt((duration + stop) * 60));
            targetStartedDowned = attack.CurrentTarget.Pawn?.Downed ?? false;
        }

        //保存已执行次数和连击节奏，读档不会重放已经结算的刺击。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref moveId, "meleeMove"); Scribe_Values.Look(ref facing, "meleeFacing");
            Scribe_Values.Look(ref windupTicks, "meleeWindupTicks"); Scribe_Values.Look(ref elapsedTicks, "meleeElapsedTicks");
            Scribe_Values.Look(ref duration, "meleeDuration"); Scribe_Values.Look(ref contact, "meleeContact");
            Scribe_Values.Look(ref aimAngle, "meleeAimAngle"); Scribe_Values.Look(ref stop, "meleeStop");
            Scribe_Values.Look(ref targetStartedDowned, "meleeTargetStartedDowned");
            Scribe_Values.Look(ref strikeCount, "meleeStrikeCount"); Scribe_Values.Look(ref strikeInterval, "meleeStrikeInterval");
            Scribe_Values.Look(ref impactsResolved, "meleeImpactsResolved");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) restoreVisuals = true;
        }

        //正常受击、目标移动和 AI 换工作不重置时钟，刀刃始终完成当前轨迹。
        public override void StanceTick()
        {
            var owner = Owner;
            if (!Committed) { Finish(owner, cancelAnimation: true); return; }
            float now = Find.TickManager.TicksGame / 60f;
            if (restoreVisuals)
            {
                var move = owner.Definition.moves.First(m => m.id == moveId);
                owner.Playback.Start(move, facing, now, false, duration);
                owner.Playback.AimAngle = aimAngle; owner.Playback.Stop = stop;
                restoreVisuals = false;
            }
            ticksLeft--; elapsedTicks++;
            float elapsed = elapsedTicks <= windupTicks ? elapsedTicks / (float)windupTicks * duration * contact
                : duration * contact + (elapsedTicks - windupTicks) / 60f;
            owner.Playback.Seek(elapsed, now);
            if (impactsResolved < strikeCount && elapsedTicks >= windupTicks + Mathf.RoundToInt(impactsResolved * strikeInterval * 60))
                ResolveImpact(owner);
            if (!Committed) { Finish(owner, cancelAnimation: !Pawn.Spawned || Pawn.Dead || Pawn.Downed); return; }
            Pawn.Rotation = facing;
            if (focusTarg == Pawn.mindState.enemyTarget) Pawn.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;
        }

        //普通姿态切换要等这一刀结束；原版伤害释放产生的冷却合并进同一个周期。
        internal bool AllowReplacement(Stance next)
        {
            if (!Committed) return true;
            if (releasingImpact && next is Stance_Cooldown cooldown && cooldown.verb == verb)
                ticksLeft = Mathf.Max(ticksLeft, cooldown.ticksLeft - elapsedTicks);
            return false;
        }

        //每次出刺只结算一次，目标离开或失效时挥空，不寻找替代目标。
        private void ResolveImpact(CompMeleeAnimation owner)
        {
            impactsResolved++;
            if (!focusTarg.HasThing || !focusTarg.Thing.Spawned || focusTarg.Thing.Map != Pawn.Map
                || focusTarg.Thing.Destroyed || verb.CurrentTarget != focusTarg || !verb.CanHitTarget(focusTarg)
                || !targetStartedDowned && focusTarg.Pawn != null && focusTarg.Pawn.Downed)
            { owner.Playback.Stop = stop = 0; return; }
            releasingImpact = true; owner.ResolvingVerb = verb;
            try { verb.WarmupComplete(); }
            finally
            {
                releasingImpact = false; owner.ResolvingVerb = null;
                stop = owner.Playback.Stop;
            }
        }

        //周期结束或实体失去行动条件后明确解除忙碌，不留下不可攻击的悬挂状态。
        private void Finish(CompMeleeAnimation owner, bool cancelAnimation)
        {
            ending = true;
            if (cancelAnimation) owner.Playback.Clear();
            if (stanceTracker.curStance == this) stanceTracker.SetStance(new Stance_Mobile());
        }
    }
}
