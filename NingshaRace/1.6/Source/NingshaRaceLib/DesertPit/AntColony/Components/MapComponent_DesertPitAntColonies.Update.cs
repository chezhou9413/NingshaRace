using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：集中实现蚁群地图组件的周期清理、入侵者缓存和警报波次推进。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：清理失效实体、刷新各巢群入侵者并推进完整警报波次。
        private void UpdateColonies(int ticks)
        {
            for (int i = colonies.Count - 1; i >= 0; i--)
            {
                AntColonyState state = colonies[i];
                PruneInvalidMembers(state);
                PruneDeathRecords(state, ticks);
                RefreshIntruders(state);
                TryRepairNest(state, ticks);
                TryUpgradeColony(state, ticks);
                TryDispatchInvestigation(state, ticks);

                if (IsFullAlarm(state, ticks) && ticks >= state.NextBoomWaveTick)
                {
                    SpawnBoomAntsToCap(state);
                    state.NextBoomWaveTick = ticks + Settings.boomWaveCooldownTicks;
                }

                if (state.NestDestroyed && state.Members.Count == 0)
                {
                    RemoveColonyAt(i, state);
                }
            }
        }

        //函数职责：从巢群状态和快速索引中移除已经死亡或销毁的成员。
        private void PruneInvalidMembers(AntColonyState state)
        {
            for (int i = state.Members.Count - 1; i >= 0; i--)
            {
                Pawn member = state.Members[i];
                if (member != null && !member.Destroyed && !member.Dead)
                {
                    continue;
                }

                state.Members.RemoveAt(i);
                if (member != null)
                {
                    coloniesByPawn.Remove(member);
                    ReleaseForageAssignments(member);
                }
            }

            if (state.Queen == null || state.Queen.Destroyed || state.Queen.Dead)
            {
                state.Queen = null;
            }

            if (state.LastAggressor != null && (state.LastAggressor.Destroyed || state.LastAggressor.Dead))
            {
                state.LastAggressor = null;
            }
        }

        //函数职责：缓存进入巢群领地的外来 Pawn、敌方蚁穴和原版虫巢，供多个成员共享目标查询结果。
        private void RefreshIntruders(AntColonyState state)
        {
            state.Intruders.Clear();
            float radius = state.Frenzy ? Settings.frenzyRadius : Settings.alertRadius;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (candidate == null || candidate.Dead || IsColonyMember(candidate, state))
                {
                    continue;
                }

                if (candidate.Position.DistanceTo(state.NestPosition) <= radius)
                {
                    state.Intruders.Add(candidate);
                }
            }

            AddEnemyAntNests(state, radius);
            AddVanillaHives(state, radius);

            Pawn aggressor = state.LastAggressor;
            if (aggressor != null && aggressor.Spawned && !aggressor.Dead && !IsColonyMember(aggressor, state) &&
                aggressor.Position.DistanceTo(state.NestPosition) <= radius &&
                (state.Frenzy || IsFullAlarm(state, Find.TickManager.TicksGame)) && !state.Intruders.Contains(aggressor))
            {
                state.Intruders.Add(aggressor);
            }
        }

        //函数职责：把处于当前领地范围内且与本巢敌对的其他自定义蚁穴加入目标缓存。
        private void AddEnemyAntNests(AntColonyState state, float radius)
        {
            for (int i = 0; i < colonies.Count; i++)
            {
                AntColonyState other = colonies[i];
                Building_DesertPitAntNest nest = other.Nest;
                if (other == state || nest == null || !nest.Spawned || nest.Destroyed || !nest.HostileTo(state.Faction))
                {
                    continue;
                }

                if (nest.Position.DistanceTo(state.NestPosition) <= radius)
                {
                    state.Intruders.Add(nest);
                }
            }
        }

        //函数职责：把处于当前领地范围内且与本巢敌对的原版虫巢加入目标缓存。
        private void AddVanillaHives(AntColonyState state, float radius)
        {
            List<Thing> hives = map.listerThings.ThingsOfDef(ThingDefOf.Hive);
            for (int i = 0; i < hives.Count; i++)
            {
                Thing hive = hives[i];
                if (hive != null && hive.Spawned && !hive.Destroyed && hive.HostileTo(state.Faction) && hive.Position.DistanceTo(state.NestPosition) <= radius)
                {
                    state.Intruders.Add(hive);
                }
            }
        }

        //函数职责：从当前巢群缓存中取得离指定成员最近且能够接触的外来实体。
        private Thing FindNearestIntruder(Pawn member, AntColonyState state)
        {
            Thing result = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < state.Intruders.Count; i++)
            {
                Thing candidate = state.Intruders[i];
                Pawn pawn = candidate as Pawn;
                if (candidate == null || !candidate.Spawned || candidate.Destroyed || pawn != null && pawn.Dead)
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

        //函数职责：统计指定巢群中仍存活的某一阶级成员数量。
        private int CountCaste(AntColonyState state, AntCaste caste)
        {
            int count = 0;
            for (int i = 0; i < state.Members.Count; i++)
            {
                Pawn member = state.Members[i];
                Comp_DesertPitAntMember memberComp = member?.TryGetComp<Comp_DesertPitAntMember>();
                if (member != null && !member.Dead && !member.Destroyed && memberComp != null && memberComp.Caste == caste)
                {
                    count++;
                }
            }

            return count;
        }

        //函数职责：永久移除已经失去蚁穴且没有存活成员的巢群状态。
        private void RemoveColonyAt(int index, AntColonyState state)
        {
            colonies.RemoveAt(index);
            coloniesById.Remove(state.Id);
            if (state.Nest != null)
            {
                coloniesByNest.Remove(state.Nest);
            }
        }
    }
}
