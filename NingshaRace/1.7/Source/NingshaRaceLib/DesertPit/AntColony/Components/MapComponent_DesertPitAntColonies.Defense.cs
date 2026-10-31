using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //普通成员使用巢群缓存，外出队伍只扫描自身二十格邻域。
    public partial class MapComponent_DesertPitAntColonies
    {
        //冲锋和腾跃已开始后不被常量行为树重新派发。
        public Job TryCreateEncounterJob(Pawn pawn)
        {
            if (pawn.CurJobDef == DefOfRefs.NingshaRace_Job_AntCharge || pawn.CurJobDef == DefOfRefs.NingshaRace_Job_AntJump) return null;
            if (!TryGetColony(pawn, out AntColonyState state)) return null;
            AntCaste caste = pawn.GetComp<Comp_DesertPitAntMember>().Caste;
            if (caste == AntCaste.Worker || caste == AntCaste.Queen)
            {
                if (caste == AntCaste.Queen && IsFullAlarm(state, Find.TickManager.TicksGame))
                    return JobMaker.MakeJob(DefOfRefs.NingshaRace_Job_AntEnterNest, state.Nest);
                Thing threat = FindNearbyWorkerThreat(pawn, state);
                if (threat != null && pawn.Position.DistanceTo(threat.Position) <= Settings.workerRetreatRadius
                    && pawn.Position.DistanceTo(state.NestPosition) > 3f) return CreateReturnToNestJob(pawn, state);
                return null;
            }
            if (!IsCombatCaste(caste) && caste != AntCaste.Boom) return null;
            Thing nearest = FindNearestIntruder(pawn, state);
            if (pawn.GetLord()?.LordJob is Lords.LordJob_DesertPitAntInvestigation)
            {
                float best = nearest == null ? Settings.alertRadius * Settings.alertRadius : pawn.Position.DistanceToSquared(nearest.Position);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, Settings.alertRadius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    foreach (Thing target in cell.GetThingList(map))
                    {
                        if (!(target is Pawn enemy) || enemy.Dead || enemy.Downed || IsColonyMember(enemy, state)) continue;
                        float distance = pawn.Position.DistanceToSquared(target.Position);
                        if (distance >= best || !CanContinueColonyAttack(pawn, target) || !CanEngageIntruder(pawn, target)) continue;
                        best = distance;
                        nearest = target;
                    }
                }
            }
            if (nearest == null || !CanContinueColonyAttack(pawn, nearest)) return null;
            return caste == AntCaste.Boom ? CreateBoomAttackJob(nearest) : CreateCombatJob(pawn, nearest);
        }

        //远处搬运的工蚁也检查自身八格内的敌人，不依赖巢周二十格缓存。
        private Thing FindNearbyWorkerThreat(Pawn pawn, AntColonyState state)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, Settings.workerRetreatRadius, true))
            {
                if (!cell.InBounds(map)) continue;
                foreach (Thing thing in cell.GetThingList(map))
                    if (thing is Pawn enemy && !enemy.Dead && !enemy.Downed && !IsColonyMember(enemy, state)) return enemy;
            }
            return null;
        }

        //撤退时只打十五格内可见敌人；远程攻击者可追击至四十五格。
        public bool CanContinueColonyAttack(Pawn member, Thing target)
        {
            if (target == null || !target.Spawned || target.Map != map || target.Destroyed
                || target is Pawn victim && (victim.Dead || victim.Downed)) return false;
            if (!TryGetColony(member, out AntColonyState state)) return member.GetComp<Comp_DesertPitAntMember>()?.ColonyId == 0;
            if (IsFullAlarm(state, Find.TickManager.TicksGame) && IsCombatCaste(member.GetComp<Comp_DesertPitAntMember>().Caste)
                && target.Position.DistanceToSquared(GuardCenter(member, state)) > 144f) return false;
            if (!IsRetreating(state, Find.TickManager.TicksGame)) return true;
            bool retaliating = target == state.LastAggressor && Find.TickManager.TicksGame < state.RetaliationUntilTick;
            float radius = retaliating ? Settings.retaliationRadius : Settings.retreatDefenseRadius;
            return target.Position.DistanceToSquared(state.NestPosition) <= radius * radius
                && GenSight.LineOfSight(member.Position, target.Position, map, true);
        }
    }
}
