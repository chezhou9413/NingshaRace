using System.Collections.Generic;
using NingshaRaceLib.DesertPit.AntColony.Components;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Jobs
{
    //类职责：让工蚁花费采收时间获得植物真实产物，再由搬运系统将其入库。
    public sealed class JobDriver_DesertPitAntHarvest : JobDriver
    {
        //函数职责：独占目标植物，避免多只工蚁重复结算同一株产量。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);

        //函数职责：抵达并采收成熟植物，产物放回原地，驱蚁恢复时在收获前取消。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Map.GetComponent<MapComponent_DesertPitAntColonies>().CanHarvestForColony(pawn, TargetA.Thing as Plant));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait((int)TargetA.Thing.def.plant.harvestWork).WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(delegate
            {
                Plant plant = (Plant)TargetA.Thing;
                if (!Map.GetComponent<MapComponent_DesertPitAntColonies>().CanHarvestForColony(pawn, plant))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                int count = plant.YieldNow();
                if (count > 0)
                {
                    Thing food = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef);
                    food.stackCount = count;
                    food.SetForbidden(true, false);
                    if (!GenPlace.TryPlaceThing(food, plant.Position, Map, ThingPlaceMode.Near))
                        throw new System.InvalidOperationException("工蚁采收的食物无法放回地图。");
                }
                plant.PlantCollected(pawn, PlantDestructionMode.Chop);
                Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyFungusHarvested();
            });
        }
    }
}
