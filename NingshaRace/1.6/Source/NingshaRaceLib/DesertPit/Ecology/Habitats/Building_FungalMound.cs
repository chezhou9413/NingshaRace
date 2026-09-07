using System.Collections.Generic;
using RimWorld;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Habitats
{
    //类职责：在有限栖息范围内持续补生食用菌，并保存不会被植物覆盖的通路。
    public sealed class Building_FungalMound : Building_AntHabitat
    {
        private int nextRegrowthTick;
        private List<IntVec3> excludedCells = new List<IntVec3>();
        public DefModExtension_FungalMound Settings => def.GetModExtension<DefModExtension_FungalMound>();
        public override float Radius => Settings.radius;
        protected override bool EmitsSpores => true;

        //函数职责：保存下一次补生时间和场景预留通道，读档不重置产出进度。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextRegrowthTick, "nextFungalRegrowth");
            Scribe_Collections.Look(ref excludedCells, "fungalExcludedCells", LookMode.Value);
        }

        //函数职责：记录栖息地内的通道格并一次性生成不同成熟阶段的初始食用菌。
        public void SeedHabitat(HashSet<IntVec3> protectedCells)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, Radius, true))
                if (protectedCells.Contains(cell)) excludedCells.Add(cell);
            Replenish(Settings.targetPlants, true);
            nextRegrowthTick = Find.TickManager.TicksGame + Settings.regrowthIntervalTicks;
        }

        //函数职责：在补生时间到达时填补有限数量的菌群缺口，不产生无限堆积的物资。
        protected override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame < nextRegrowthTick) return;
            nextRegrowthTick = Find.TickManager.TicksGame + Settings.regrowthIntervalTicks;
            Replenish(Settings.regrowthBatch, false);
        }

        //函数职责：在自然空格随机疏植食用菌，同时遵守容量、间距及建筑、玩家用地、储藏和通路限制。
        private void Replenish(int budget, bool initial)
        {
            DefModExtension_FungalMound settings = Settings;
            List<IntVec3> empty = new List<IntVec3>();
            int count = 0;
            MapComponent_DesertPitAntColonies ants = Map.GetComponent<MapComponent_DesertPitAntColonies>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, Radius, true))
            {
                if (!cell.InBounds(Map)) continue;
                if (cell.GetPlant(Map) != null) count++;
                else if (!excludedCells.Contains(cell) && !ants.IsColonyStorageCell(cell)
                    && DesertPitPlantEcologyUtility.CanPlacePlant(Map, cell, DefOfRefs.NingshaRace_DesertPitPlantA, !initial)) empty.Add(cell);
            }
            int amount = System.Math.Min(budget, settings.targetPlants - count);
            if (amount <= 0 || empty.Count == 0) return;

            //随机候选顺序保留自然疏密变化，不按固定行列或棋盘格排列菌群。
            empty.Shuffle();
            int spacingCellCount = GenRadial.NumCellsInRadius(settings.minimumPlantSpacing);
            float spacingSquared = settings.minimumPlantSpacing * settings.minimumPlantSpacing;
            for (int i = 0; i < empty.Count && amount > 0; i++)
            {
                //每株落地前读取周围实际植物，同批次刚生成的菌子和范围外邻株也参与避让。
                if (!SpacingAllows(empty[i], spacingCellCount, spacingSquared)) continue;
                DesertPitPlantEcologyUtility.SpawnPlant(Map,
                    Rand.Bool ? DefOfRefs.NingshaRace_DesertPitPlantA : DefOfRefs.NingshaRace_DesertPitPlantC,
                    empty[i], initial ? new FloatRange(0.55f, 1f) : new FloatRange(0.05f, 0.15f));
                amount--;
            }
        }

        //函数职责：仅检查候选格附近的有限圆形邻域，拒绝与现有植物距离过近的位置。
        private bool SpacingAllows(IntVec3 cell, int spacingCellCount, float spacingSquared)
        {
            for (int i = 0; i < spacingCellCount; i++)
            {
                IntVec3 offset = GenRadial.RadialPattern[i];
                if (offset.LengthHorizontalSquared >= spacingSquared) continue;
                IntVec3 nearby = cell + offset;
                if (nearby.InBounds(Map) && nearby.GetPlant(Map) != null) return false;
            }
            return true;
        }

        //函数职责：说明自然补生和加速范围，不向玩家暴露内部计时字段。
        public override string GetInspectString() => InspectPrefix() + "附近植物生长速度：" + Settings.growthMultiplier.ToString("0.#")
            + "倍\n食用菌收成：" + Settings.harvestMultiplier.ToString("0.#") + "倍\n作用范围：" + Radius.ToString("0.#") + "格\n空地上会逐渐长出食用菌";
    }
}
