using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.Generation;
using NingshaRaceLib.DesertPit.Antlion.Holders;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Components
{
    //类职责：按生态配置定时补充缺失蚁狮，保存检查时间，避免读档重复补充或数量失控。
    public sealed class MapComponent_AntlionPopulation : MapComponent
    {
        private int nextReplenishTick = -1;
        private DefModExtension_DesertPitAntlion settings;
        private readonly List<IntVec3> populationPositions = new List<IntVec3>();

        //构造职责：将蚁狮种群维护器绑定到地图。
        public MapComponent_AntlionPopulation(Map map) : base(map)
        {
        }

        //函数职责：保存下一次补充时间，读档后延续原来的检查周期。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextReplenishTick, "antlionNextReplenishTick", -1);
        }

        //函数职责：只在具有蚁狮生态配置的地图启用维护，并安排首次检查。
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            settings = map.Biome.GetModExtension<DefModExtension_DesertPitAntlion>();
            if (settings != null && nextReplenishTick < 0)
                nextReplenishTick = Find.TickManager.TicksGame + settings.replenishIntervalTicks;
        }

        //函数职责：每个周期统计全部存活蚁狮，低于配置上限时最多补充一个地下个体。
        public override void MapComponentTick()
        {
            if (settings == null || settings.countRange.max <= 0) return;
            int now = Find.TickManager.TicksGame;
            if (now < nextReplenishTick) return;
            //先推进计时，种群已满或无合法位置时也等待下一轮，避免逐 Tick 重试。
            nextReplenishTick = now + settings.replenishIntervalTicks;
            if (AntlionPopulationUtility.CollectPopulation(map, populationPositions) >= settings.countRange.max) return;
            var behavior = DefOfRefs.NingshaRace_Antlion.GetCompProperties<CompProperties_AntlionAmbush>();
            IntVec3 cell = AntlionPopulationUtility.FindReplenishmentCell(map, settings, behavior, populationPositions);
            if (!cell.IsValid) return;
            Pawn pawn = PawnGenerator.GeneratePawn(DefOfRefs.NingshaRace_AntlionKind, AntlionFactionUtility.GetOrCreate());
            AntlionBurrow.Store(pawn, map, cell, now + behavior.rearmTicks);
        }
    }
}
