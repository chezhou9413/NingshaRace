using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Combat;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.AntColony.AI
{
    //类职责：在常规可达目标搜索之前，为封闭蚁巢创建原版破障工作。
    public sealed class JobGiver_DesertPitAntPassage : ThinkNode_JobGiver
    {
        //函数职责：检测返巢或出巢通路，逐层清除路径上最先遇到的障碍。
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Downed || pawn.Dead || pawn.InMentalState) return null;
            if (pawn.GetLord()?.CurLordToil is LordToil_Sleep) return null;
            Comp_DesertPitAntMember member = pawn.TryGetComp<Comp_DesertPitAntMember>();
            if (member == null || member.Caste == AntCaste.Boom) return null;
            int now = Find.TickManager.TicksGame;
            if (now < member.NextPassageCheckTick) return null;
            member.NextPassageCheckTick = now + 240 + pawn.thingIDNumber % 90;
            MapComponent_DesertPitAntColonies manager = pawn.Map.GetComponent<MapComponent_DesertPitAntColonies>();
            if (!manager.TryGetColony(pawn, out AntColonyState state) || state.NestDestroyed
                || state.Nest == null || !state.Nest.Spawned) return null;
            if (pawn.TryGetComp<CompCanBeDormant>() is CompCanBeDormant dormant && !dormant.Awake) return null;

            //能返回巢穴时检查通往公共洞道的出口；被隔在外面的成员优先打通返巢路。
            bool canReturn = pawn.CanReach(state.Nest, PathEndMode.Touch, Danger.Deadly);
            if (canReturn && (member.Caste == AntCaste.Queen || now < state.RetreatUntilTick)) return null;
            LocalTargetInfo target = canReturn ? new LocalTargetInfo(state.PassageAnchor) : new LocalTargetInfo(state.Nest);
            PathEndMode endMode = canReturn ? PathEndMode.OnCell : PathEndMode.Touch;
            if (!target.IsValid || !target.Cell.InBounds(pawn.Map)
                || pawn.CanReach(target, endMode, Danger.Deadly)) return null;

            PathFinderCostTuning tuning = PathFinderCostTuning.For(pawn);
            //优先清理原洞道上的封墙，避免为了直线距离穿凿厚实天然岩层。
            tuning.costBlockedWallExtraForNaturalWalls = 2000;
            using (PawnPath path = pawn.Map.pathFinder.FindPathNow(pawn.Position, target,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings), tuning, endMode))
            {
                Thing blocker = path.FirstBlockingBuilding(out IntVec3 before, pawn);
                if (blocker == null || blocker == state.Nest || !AntMeleeCapability.CanAttack(pawn, blocker)
                    || manager.IsWorkerForageDangerous(pawn, before)) return null;
                Job job = DigUtility.PassBlockerJob(pawn, blocker, before, true, false);
                if (job?.def == JobDefOf.AttackMelee) job.def = DefOfRefs.NingshaRace_Job_DesertPitAntMelee;
                return job;
            }
        }
    }
}
