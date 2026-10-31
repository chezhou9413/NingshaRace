using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Components;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //工蚁使用原版采矿伤害和六级采矿间隔，避免动物无操作能力降低工作速度。
    public sealed class JobDriver_AntMine : JobDriver
    {
        private int nextHit;

        //阻挡物由原版预留系统独占。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed)
            && Map.GetComponent<MapComponent_DesertPitAntColonies>().RegisterMiningTask(pawn, TargetA.Thing);

        //原版采矿每击天然岩石八十点、其他障碍四十点，六级速度为 0.76。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var manager = Map.GetComponent<MapComponent_DesertPitAntColonies>();
            var task = manager.TaskFor(pawn);
            AddFinishAction(condition => manager.ReleaseTask(pawn, task));
            this.FailOn(() => !manager.TaskIsValid(pawn));
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil mine = ToilMaker.MakeToil("工蚁破障");
            mine.defaultCompleteMode = ToilCompleteMode.Never;
            mine.tickAction = delegate
            {
                if (++nextHit < 132) return;
                nextHit = 0;
                Thing target = TargetA.Thing;
                int damage = target.def.building.isNaturalRock ? 80 : 40;
                if (target is Mineable ore && target.HitPoints <= damage)
                {
                    ore.Notify_TookMiningDamage(target.HitPoints, pawn);
                    ore.HitPoints = 0;
                    ore.DestroyMined(pawn);
                }
                else target.TakeDamage(new DamageInfo(DamageDefOf.Mining, damage, instigator: pawn));
                if (target.Destroyed) EndJobWith(JobCondition.Succeeded);
            };
            mine.WithEffect(EffecterDefOf.Mine, TargetIndex.A);
            mine.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return mine;
        }

        //存档保留本次敲击前的进度。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextHit, "nextHit");
        }
    }
}
