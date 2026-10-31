using System.Linq;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.Lords;
using NingshaRaceLib.DesertPit.AntColony.State;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //调查结果由领主结束事件上报，复仇在下一次巢群更新派遣。
    public partial class MapComponent_DesertPitAntColonies
    {
        //全灭和返巢不足两只都保存一次复仇地点。
        public void NotifyInvestigationEnded(int id, IntVec3 hotspot, int returned)
        {
            if (returned < 2 && coloniesById.TryGetValue(id, out AntColonyState state) && state.CurrentLevel >= 3)
                state.RevengeTarget = hotspot;
        }

        //可行动的飞镰、吐酸、破军蚁全部参加，普通兵蚁留守。
        private bool TryDispatchRevenge(AntColonyState state, int now)
        {
            if (!state.RevengeTarget.IsValid || IsFullAlarm(state, now) || IsRetreating(state, now)
                || HasActiveInvestigation(state) || ColonyDormant(state)) return false;
            var squad = state.Members.Where(pawn => pawn.Spawned && !pawn.Dead && !pawn.Downed
                && !pawn.InMentalState && pawn.GetLord() == null
                && (pawn.GetComp<Comp_DesertPitAntMember>().Caste == AntCaste.Flying
                    || pawn.GetComp<Comp_DesertPitAntMember>().Caste == AntCaste.Acid
                    || pawn.GetComp<Comp_DesertPitAntMember>().Caste == AntCaste.Breaker)).ToList();
            if (squad.Count == 0) return false;
            IntVec3 target = state.RevengeTarget;
            state.RevengeTarget = IntVec3.Invalid;
            foreach (Pawn pawn in squad) pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            LordMaker.MakeNewLord(state.Faction, new LordJob_DesertPitAntInvestigation(state.Id, state.NestPosition,
                target, Settings.investigationRallyTimeoutTicks, Settings.investigationTravelTimeoutTicks, 4500)
                { Revenge = true }, map, squad);
            return true;
        }
    }
}
