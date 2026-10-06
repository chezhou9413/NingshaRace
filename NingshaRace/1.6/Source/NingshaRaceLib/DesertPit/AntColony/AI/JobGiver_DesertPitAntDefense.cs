using NingshaRaceLib.DesertPit.AntColony.Components;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.AI
{
    //兵蚁在集结、调查和返程途中优先处理眼前敌人。
    public sealed class JobGiver_DesertPitAntDefense : ThinkNode_JobGiver
    {
        //将局部遇敌请求交给巢群组件，不改变原版倒地和着火响应。
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.InMentalState || pawn.IsBurning()) return null;
            return pawn.Map.GetComponent<MapComponent_DesertPitAntColonies>().TryCreateEncounterJob(pawn);
        }
    }
}
