using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.Lords;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：处理常规蚂蚁伤亡记录、四小时撤退与一天死亡热点调查队派遣。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：记录工蚁或兵蚁死亡，并在未处于撤退时检查四小时百分之五十伤亡阈值。
        private void RecordRegularAntDeath(AntColonyState state, Pawn pawn, AntCaste caste)
        {
            if (caste != AntCaste.Worker && caste != AntCaste.Soldier)
            {
                return;
            }

            int ticks = Find.TickManager.TicksGame;
            AntDeathRecord record = new AntDeathRecord(ticks, pawn.PositionHeld)
            {
                CountedForRetreat = IsRetreating(state, ticks)
            };
            state.DeathRecords.Add(record);
            if (!IsRetreating(state, ticks))
            {
                TryTriggerRetreat(state, ticks);
            }
        }

        //函数职责：按当前存活常规蚁加窗口死亡数计算向上取整的百分之五十撤退门槛。
        private void TryTriggerRetreat(AntColonyState state, int ticks)
        {
            int windowStart = ticks - Settings.retreatLossWindowTicks;
            List<AntDeathRecord> recent = state.DeathRecords.Where(record => record.Tick >= windowStart).ToList();
            int uncountedDeaths = recent.Count(record => !record.CountedForRetreat);
            int aliveRegular = CountCaste(state, AntCaste.Worker) + CountCaste(state, AntCaste.Soldier);
            int threshold = Mathf.CeilToInt((aliveRegular + recent.Count) * Settings.retreatLossFraction);
            if (uncountedDeaths < Mathf.Max(1, threshold))
            {
                return;
            }

            for (int i = 0; i < recent.Count; i++)
            {
                recent[i].CountedForRetreat = true;
            }

            state.RetreatUntilTick = ticks + Settings.retreatDurationTicks;
            CancelInvestigation(state);
            InterruptAllMembers(state);
        }

        //函数职责：报告指定巢群当前是否仍处于伤亡撤退阶段。
        public bool IsRetreating(AntColonyState state, int ticks)
        {
            return state != null && !state.NestDestroyed && ticks < state.RetreatUntilTick;
        }

        //函数职责：清理超过一天的死亡记录，控制存档体积与热点查询成本。
        private void PruneDeathRecords(AntColonyState state, int ticks)
        {
            int oldestTick = ticks - Settings.investigationLossWindowTicks;
            state.DeathRecords.RemoveAll(record => record == null || record.Tick < oldestTick);
        }

        //函数职责：在满足安全条件、死亡热点和兵蚁数量时建立唯一的 Lord 调查队。
        private void TryDispatchInvestigation(AntColonyState state, int ticks)
        {
            if (state.NestDestroyed || state.Nest == null || state.Nest.Destroyed || state.Frenzy || IsRetreating(state, ticks) || IsFullAlarm(state, ticks) || ticks < state.NextInvestigationTick || HasActiveInvestigation(state))
            {
                return;
            }

            IntVec3 hotspot;
            List<AntDeathRecord> hotspotRecords;
            if (!TryFindDeathHotspot(state, ticks, out hotspot, out hotspotRecords))
            {
                return;
            }

            List<Pawn> soldiers = state.Members
                .Where(member => IsAvailableInvestigationSoldier(member))
                .OrderBy(member => member.Position.DistanceToSquared(state.NestPosition))
                .ToList();
            int desiredCount = Math.Min(Math.Min(soldiers.Count, 2 + state.CurrentLevel), Settings.investigationMaxSquadSize);
            if (desiredCount < 2)
            {
                return;
            }

            List<Pawn> squad = soldiers.GetRange(0, desiredCount);
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad[i].CurJobDef != null)
                {
                    squad[i].jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }

            LordJob_DesertPitAntInvestigation job = new LordJob_DesertPitAntInvestigation(
                state.Id,
                state.NestPosition,
                hotspot,
                Settings.investigationRallyTimeoutTicks,
                Settings.investigationTravelTimeoutTicks,
                Settings.investigationDefendTicks);
            LordMaker.MakeNewLord(state.Faction, job, map, squad);
            for (int i = 0; i < hotspotRecords.Count; i++)
            {
                hotspotRecords[i].CountedForInvestigation = true;
            }

            state.NextInvestigationTick = ticks + Settings.investigationCooldownTicks;
        }

        //函数职责：从一天内未消费的死亡记录中寻找达到十格聚类门槛的热点。
        private bool TryFindDeathHotspot(AntColonyState state, int ticks, out IntVec3 hotspot, out List<AntDeathRecord> hotspotRecords)
        {
            int oldestTick = ticks - Settings.investigationLossWindowTicks;
            List<AntDeathRecord> candidates = state.DeathRecords
                .Where(record => record.Tick >= oldestTick && !record.CountedForInvestigation)
                .ToList();
            int threshold = Math.Max(Settings.investigationMinimumDeaths, Mathf.CeilToInt(state.Population.RegularAntCap * Settings.investigationLossFraction));
            for (int i = 0; i < candidates.Count; i++)
            {
                AntDeathRecord center = candidates[i];
                List<AntDeathRecord> cluster = candidates
                    .Where(record => record.Position.DistanceTo(center.Position) <= Settings.investigationHotspotRadius)
                    .ToList();
                if (cluster.Count >= threshold)
                {
                    hotspot = center.Position;
                    hotspotRecords = cluster;
                    return true;
                }
            }

            hotspot = IntVec3.Invalid;
            hotspotRecords = null;
            return false;
        }

        //函数职责：判断兵蚁存活、可行动、未处于精神状态且尚未被其他 Lord 接管。
        private static bool IsAvailableInvestigationSoldier(Pawn pawn)
        {
            Comp_DesertPitAntMember comp = pawn?.TryGetComp<Comp_DesertPitAntMember>();
            return pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.MentalStateDef == null && pawn.GetLord() == null && comp != null && comp.Caste == AntCaste.Soldier;
        }

        //函数职责：判断指定蚁巢当前是否已经拥有一支调查队。
        private bool HasActiveInvestigation(AntColonyState state)
        {
            return map.lordManager.lords.Any(lord => lord.LordJob is LordJob_DesertPitAntInvestigation job && job.ColonyId == state.Id);
        }

        //函数职责：立即移除指定蚁巢的全部调查 Lord，使成员恢复个体 ThinkTree。
        private void CancelInvestigation(AntColonyState state)
        {
            for (int i = map.lordManager.lords.Count - 1; i >= 0; i--)
            {
                Lord lord = map.lordManager.lords[i];
                if (lord.LordJob is LordJob_DesertPitAntInvestigation job && job.ColonyId == state.Id)
                {
                    map.lordManager.RemoveLord(lord);
                }
            }
        }

        //函数职责：强制中断巢群成员当前的外出、搬运、攻击和调查工作。
        private static void InterruptAllMembers(AntColonyState state)
        {
            for (int i = 0; i < state.Members.Count; i++)
            {
                Pawn pawn = state.Members[i];
                if (pawn != null && pawn.Spawned && !pawn.Dead && pawn.CurJobDef != null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
        }

        //函数职责：在撤退阶段仅让兵蚁与爆浆蚁攻击进入蚁穴六格内的敌人，其余成员返巢等待。
        private Job TryCreateRetreatJob(Pawn pawn, AntColonyState state, AntCaste caste)
        {
            Thing nearbyIntruder = FindNearestIntruderWithinNestRadius(pawn, state, Settings.retreatDefenseRadius);
            if ((caste == AntCaste.Soldier || caste == AntCaste.Boom) && nearbyIntruder != null)
            {
                return caste == AntCaste.Boom ? CreateBoomAttackJob(nearbyIntruder) : CreateMeleeAttackJob(nearbyIntruder);
            }

            if (pawn.Position.DistanceTo(state.NestPosition) > Settings.retreatDefenseRadius)
            {
                return CreateReturnToNestJob(pawn, state);
            }

            return JobMaker.MakeJob(JobDefOf.Wait, 120);
        }

        //函数职责：寻找进入蚁穴指定半径且当前成员能够抵达的最近敌对实体。
        private Thing FindNearestIntruderWithinNestRadius(Pawn member, AntColonyState state, float radius)
        {
            Thing result = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < state.Intruders.Count; i++)
            {
                Thing candidate = state.Intruders[i];
                if (candidate == null || !candidate.Spawned || candidate.Destroyed || candidate.Position.DistanceTo(state.NestPosition) > radius)
                {
                    continue;
                }

                float distance = member.Position.DistanceToSquared(candidate.Position);
                if (distance < bestDistance && member.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                {
                    bestDistance = distance;
                    result = candidate;
                }
            }

            return result;
        }
    }
}
