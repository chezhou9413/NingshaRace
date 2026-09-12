using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using NingshaRaceLib.DesertPit.AntColony.Combat;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：集中处理有限受击反击及吐酸蚁的射击位置与近远程切换。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：成员受到实际伤害时记住攻击者，只在反击目标变化或记忆过期时中断全巢工作。
        public void NotifyMemberDamaged(Pawn member, Thing aggressor)
        {
            if (!TryGetColony(member, out AntColonyState state) || !IsValidAggressor(state, aggressor)) return;
            bool changed = state.LastAggressor != aggressor || Find.TickManager.TicksGame >= state.RetaliationUntilTick;
            RememberAggressor(state, aggressor);
            if (changed)
            {
                CancelInvestigation(state);
                InterruptAllMembers(state);
            }
        }

        //函数职责：仅记录同图、存活、非同巢且处于有限反击距离内的攻击来源。
        private bool IsValidAggressor(AntColonyState state, Thing aggressor)
        {
            return aggressor != null && aggressor.Spawned && aggressor.Map == map && !aggressor.Destroyed
                && !(aggressor is Pawn pawn && (pawn.Dead || pawn.Downed || IsColonyMember(pawn, state)))
                && aggressor != state.Nest
                && aggressor.Position.DistanceToSquared(state.NestPosition) <= Settings.retaliationRadius * Settings.retaliationRadius;
        }

        //函数职责：保留一段时间的受击目标，不让下一次领地扫描立即忘掉范围外射手。
        private void RememberAggressor(AntColonyState state, Thing aggressor)
        {
            if (!IsValidAggressor(state, aggressor)) return;
            state.LastAggressor = aggressor;
            state.RetaliationUntilTick = Find.TickManager.TicksGame + Settings.retaliationDurationTicks;
            if (!state.Intruders.Contains(aggressor)) state.Intruders.Add(aggressor);
        }

        //函数职责：取吐酸蚁仍可使用的原生远程攻击，口器受损后不强行发射。
        public static Verb AcidVerb(Pawn pawn)
        {
            if (pawn.TryGetComp<Comp_DesertPitAntMember>()?.Caste != AntCaste.Acid) return null;
            foreach (Verb verb in pawn.verbTracker.AllVerbs)
                if (!verb.IsMeleeAttack && verb.Available()) return verb;
            return null;
        }

        //函数职责：允许吐酸蚁隔障碍射击当前可命中的敌人，其他情况仍要求步行接近目标。
        private static bool CanEngageIntruder(Pawn pawn, Thing target)
        {
            //缓存刷新之间也检查倒地状态，撤退防御与常规进攻都不追击失去行动能力的目标。
            if (target == null || !target.Spawned || target.Map != pawn.Map || target.Destroyed
                || target is Pawn victim && (victim.Dead || victim.Downed)) return false;
            Verb verb = AcidVerb(pawn);
            return verb != null && verb.CanHitTarget(target) || pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly);
        }

        //函数职责：贴身时使用颚齿，远处先寻找合法射击点，再完成一次有实际弹丸的喷酸。
        private Job CreateCombatJob(Pawn pawn, Thing target)
        {
            Verb verb = AcidVerb(pawn);
            if (verb == null || pawn.Position.AdjacentTo8WayOrInside(target.Position))
                return AntMeleeCapability.CanAttack(pawn, target) ? CreateMeleeAttackJob(target) : null;
            if (verb.CanHitTarget(target))
            {
                Job attack = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
                attack.verbToUse = verb;
                attack.maxNumStaticAttacks = 1;
                attack.expiryInterval = 500;
                attack.endIfCantShootTargetFromCurPos = true;
                return attack;
            }
            if (CastPositionFinder.TryFindCastPosition(new CastPositionRequest
            {
                caster = pawn, target = target, verb = verb,
                maxRangeFromTarget = verb.verbProps.range,
                maxRangeFromCaster = Settings.retaliationRadius,
                wantCoverFromTarget = false
            }, out IntVec3 position) && position != pawn.Position)
            {
                Job move = JobMaker.MakeJob(JobDefOf.Goto, position);
                move.expiryInterval = 250;
                move.locomotionUrgency = LocomotionUrgency.Jog;
                return move;
            }
            return JobMaker.MakeJob(JobDefOf.Wait, 90);
        }
    }
}
