using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Buildings;
using NingshaRaceLib.DesertPit.AntColony.Components;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //先从容器取出真实食物，再交给原版进食流程。
    public sealed class JobDriver_AntTakeFood : JobDriver
    {
        //同一时间仅一只成员在容器入口取食。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);

        //食物若已被搬走则取消，不生成替代品。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Do(delegate
            {
                Building_AntColonyStructure source = (Building_AntColonyStructure)TargetA.Thing;
                Thing food = TargetB.Thing;
                if (!source.Contents.Contains(food)) { EndJobWith(JobCondition.Incompletable); return; }
                int count = food is Corpse ? 1 : System.Math.Min(food.stackCount,
                    FoodUtility.WillIngestStackCountOf(pawn, food.def, FoodUtility.NutritionForEater(pawn, food)));
                if (!source.Contents.TryDrop(food, pawn.Position, Map, ThingPlaceMode.Near, count, out Thing dropped))
                { EndJobWith(JobCondition.Incompletable); return; }
                Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyInventoryChanged(source.ColonyId);
                Job ingest = JobMaker.MakeJob(JobDefOf.Ingest, dropped);
                ingest.count = count;
                pawn.jobs.jobQueue.EnqueueFirst(ingest);
            });
        }
    }
}
