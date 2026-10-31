using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Components;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Buildings
{
    //每塔独立按周围食用菌数量生产，库存满时暂停生产时钟。
    public sealed class Building_AntHoneyTower : Building_AntColonyStructure
    {
        private int productionProgress;
        private int nextRegrowthTick;
        private int targetPlants;
        public override float Radius => Settings.growthRadius;
        protected override bool EmitsSpores => true;

        //初始菌群随机七至十六株，后续补生保持同一目标数。
        public void SeedHabitat()
        {
            targetPlants = Rand.RangeInclusive(7, 16);
            Replenish(true);
            nextRegrowthTick = Find.TickManager.TicksGame + Settings.regrowthTicks;
        }

        //生产只在所属巢群存活且已经苏醒时运行。
        protected override void Tick()
        {
            base.Tick();
            if (!this.IsHashIntervalTick(60) || ColonyId <= 0) return;
            MapComponent_DesertPitAntColonies manager = Map.GetComponent<MapComponent_DesertPitAntColonies>();
            if (!manager.StructureCanOperate(ColonyId)) return;
            int stored = Contents.Sum(thing => thing.stackCount);
            if (stored < Settings.productionCap)
            {
                productionProgress += 60;
                if (productionProgress >= Settings.productionTicks)
                {
                    productionProgress -= Settings.productionTicks;
                    Thing honey = ThingMaker.MakeThing(DefOfRefs.NingshaRace_EmeraldHoneydew);
                    honey.stackCount = System.Math.Min(Settings.productionCap - stored,
                        (MushroomCount() + 1) * Settings.productionPerPlant);
                    if (!Contents.TryAdd(honey)) throw new System.InvalidOperationException("蜜露塔无法存放产物：" + this);
                    manager.NotifyInventoryChanged(ColonyId);
                }
            }
            if (Find.TickManager.TicksGame >= nextRegrowthTick)
            {
                nextRegrowthTick = Find.TickManager.TicksGame + Settings.regrowthTicks;
                Replenish(false);
            }
        }

        //只计入食用菌 A、C，邻塔重叠可各自计产量。
        private int MushroomCount() => GenRadial.RadialCellsAround(Position, Radius, true)
            .Count(cell => cell.InBounds(Map) && IsMushroom(cell.GetPlant(Map)));
        private static bool IsMushroom(Plant plant) => plant != null
            && (plant.def == DefOfRefs.NingshaRace_DesertPitPlantA || plant.def == DefOfRefs.NingshaRace_DesertPitPlantC);

        //在自然空地补生，避开储藏、建筑、物资与玩家区域。
        private void Replenish(bool initial)
        {
            int missing = targetPlants - MushroomCount();
            if (missing <= 0) return;
            List<IntVec3> cells = GenRadial.RadialCellsAround(Position, Radius, true).ToList();
            cells.Shuffle();
            MapComponent_DesertPitAntColonies manager = Map.GetComponent<MapComponent_DesertPitAntColonies>();
            foreach (IntVec3 cell in cells)
            {
                if (!cell.InBounds(Map) || !cell.Standable(Map) || cell.GetTerrain(Map).IsWater || cell.GetEdifice(Map) != null
                    || cell.GetPlant(Map) != null || manager.IsColonyStorageCell(cell)
                    || Map.zoneManager.ZoneAt(cell) != null || Map.areaManager.Home[cell]
                    || cell.GetThingList(Map).Any(thing => thing.def.category == ThingCategory.Item || thing is Building)) continue;
                ThingDef plantDef = Rand.Bool ? DefOfRefs.NingshaRace_DesertPitPlantA : DefOfRefs.NingshaRace_DesertPitPlantC;
                //巢区菌膜自带肥力，区域外的贫瘠自然地面沿用菌床供养。
                if (cell.GetTerrain(Map).fertility < plantDef.plant.fertilityMin) Map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
                Plant plant = (Plant)ThingMaker.MakeThing(plantDef);
                plant.Growth = initial ? Rand.Range(0.55f, 1f) : Rand.Range(0.05f, 0.15f);
                GenSpawn.Spawn(plant, cell, Map);
                if (--missing == 0) break;
            }
        }

        //生产进度和菌群目标随存档保存，读档不重复初始播种。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref productionProgress, "productionProgress");
            Scribe_Values.Look(ref nextRegrowthTick, "nextRegrowthTick");
            Scribe_Values.Look(ref targetPlants, "targetPlants");
        }

        //按实际蜜露数量展示产出条件，原版说明为空时直接显示首行。
        public override string GetInspectString() => InspectPrefix() + "蜜露：" + Contents.Sum(thing => thing.stackCount)
            + " / " + Settings.productionCap + "\n附近食用菌：" + MushroomCount() + "\n植物生长速度：" + Settings.growthMultiplier + "倍";
    }
}
