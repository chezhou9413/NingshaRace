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
        //目标 B 指明本次采收是否用于当前蚂蚁直接进食。
        private bool ForEating => TargetB.Thing == pawn;

        //函数职责：独占目标植物，避免多只工蚁重复结算同一株产量。
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);

        //函数职责：抵达并采收成熟植物，产物放回原地，驱蚁恢复时在收获前取消。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Map.GetComponent<MapComponent_DesertPitAntColonies>().CanHarvestForColony(pawn, TargetA.Thing as Plant, ForEating));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil harvest = Toils_General.Wait((int)TargetA.Thing.def.plant.harvestWork)
                .WithProgressBarToilDelay(TargetIndex.A);
            harvest.WithEffect(TargetA.Thing.def.plant.IsTree ? EffecterDefOf.Harvest_Tree : EffecterDefOf.Harvest_Plant,
                TargetIndex.A);
            harvest.PlaySustainerOrSound(() => TargetA.Thing.def.plant.soundHarvesting);
            yield return harvest;
            yield return Toils_General.Do(delegate
            {
                Plant plant = (Plant)TargetA.Thing;
                if (!Map.GetComponent<MapComponent_DesertPitAntColonies>().CanHarvestForColony(pawn, plant, ForEating))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                int count = plant.YieldNow();
                Thing placedFood = null;
                if (count > 0)
                {
                    Thing food = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef);
                    food.stackCount = count;
                    food.SetForbidden(true, false);
                    if (!GenPlace.TryPlaceThing(food, plant.Position, Map, ThingPlaceMode.Near, out placedFood))
                        throw new System.InvalidOperationException("工蚁采收的食物无法放回地图。");
                }
                plant.PlantCollected(pawn, PlantDestructionMode.Chop);
                Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyFungusHarvested(pawn, placedFood, ForEating);
            });
        }
    }
}
