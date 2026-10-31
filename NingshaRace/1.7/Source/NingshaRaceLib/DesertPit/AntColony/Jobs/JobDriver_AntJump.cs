using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.Config;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //飞镰蚁复用原版 PawnFlyer，起跳和落点都遵守原版检查。
    public sealed class JobDriver_AntJump : JobDriver
    {
        //落点预留避免多只蚂蚁选择同一格。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);

        //起跳前结束当前任务，防止飞行器落地时恢复并再次起跳。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil jump = Toils_General.Do(delegate
            {
                DefModExtension_AntColony settings = DefOfRefs.NingshaRace_DesertPitAntNest.GetModExtension<DefModExtension_AntColony>();
                IntVec3 cell = TargetA.Cell;
                Comp_DesertPitAntMember member = pawn.GetComp<Comp_DesertPitAntMember>();
                if (Find.TickManager.TicksGame < member.NextAbilityTick
                    || !JumpUtility.ValidJumpTarget(pawn, Map, cell)
                    || !JumpUtility.CanHitTargetFrom(pawn, pawn.Position, cell, settings.jumpRange))
                { EndJobWith(JobCondition.Incompletable); return; }
                Map map = Map;
                IntVec3 start = pawn.Position;
                member.NextAbilityTick = Find.TickManager.TicksGame + settings.jumpCooldown;
                pawn.jobs.EndCurrentJob(JobCondition.Succeeded, startNewJob: false);
                PawnFlyer flyer = PawnFlyer.MakeFlyer(DefOfRefs.NingshaRace_AntFlyer, pawn, cell, null, null);
                GenSpawn.Spawn(flyer, start, map);
            });
            jump.atomicWithPrevious = true;
            yield return jump;
        }
    }
}
