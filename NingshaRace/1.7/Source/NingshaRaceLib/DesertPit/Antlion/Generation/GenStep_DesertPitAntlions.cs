using System.Collections;
using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.AI;
using NingshaRaceLib.DesertPit.Antlion.Components;
using NingshaRaceLib.DesertPit.Antlion.Holders;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.DesertPit.Generation.Utility;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Generation
{
    //类职责：在巨坑全部实体生成后分批筛选沙地，安置远离入口与通道的地下蚁狮。
    public sealed class GenStep_DesertPitAntlions : GenStep, IDesertPitIncrementalGenStep
    {
        //属性职责：为蚁狮生成分配独立且稳定的地图随机种子片段。
        public override int SeedPart => 921437681;

        //函数职责：在原版同步生成入口完整执行同一生成迭代器。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms)) { }
        }

        //函数职责：只扫描一次地图，分帧生成有限数量的持有容器，不在候选不足时无限重试。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            var settings = map.Biome.GetModExtension<DefModExtension_DesertPitAntlion>();
            var behavior = DefOfRefs.NingshaRace_Antlion.GetCompProperties<CompProperties_AntlionAmbush>();
            DesertPitLayoutData layout = DesertPitGenUtility.GetLayoutData();
            int desired = settings.countRange.RandomInRange;
            if (desired == 0) yield break;
            var candidates = new List<IntVec3>();
            var exits = map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_DesertPitCaveExit);
            DesertPitGenUtility.SetGenerationStatus("蚁狮：寻找隐蔽沙地");
            int scanned = 0;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (Eligible(map, cell, layout, settings, behavior, exits)) candidates.Add(cell);
                if (++scanned % settings.scanBatchSize == 0) yield return null;
            }
            candidates.Shuffle();
            var placed = new List<IntVec3>();
            for (int i = 0; i < candidates.Count && placed.Count < desired; i++)
            {
                IntVec3 cell = candidates[i];
                if (NearAny(cell, placed, settings.spacing)) continue;
                Pawn pawn = PawnGenerator.GeneratePawn(DefOfRefs.NingshaRace_AntlionKind,
                    AntlionFactionUtility.GetOrCreate());
                AntlionBurrow.Store(pawn, map, cell, Find.TickManager.TicksGame + behavior.rearmTicks);
                placed.Add(cell);
                DesertPitGenUtility.SetGenerationStatus("蚁狮：" + placed.Count + "/" + desired);
                yield return null;
            }
            if (placed.Count < desired)
                Log.Warning("[凝砂族] 沙漠巨坑可用隐蔽沙地不足，蚁狮实际生成 " + placed.Count + "/" + desired + " 只。");
        }

        //函数职责：综合实际沙地、主洞中心、出口、蚁巢和保留场景判定候选格。
        private static bool Eligible(Map map, IntVec3 cell, DesertPitLayoutData layout,
            DefModExtension_DesertPitAntlion settings, CompProperties_AntlionAmbush behavior, List<Thing> exits)
        {
            if (!AntlionSandUtility.CanBurrowAt(map, cell, behavior)
                || cell.DistanceToSquared(layout.MainCenter) < settings.entranceAvoidRadius * settings.entranceAvoidRadius
                || layout.ProtectedRouteCells.Contains(cell) || layout.ReservedSceneCells.Contains(cell)) return false;
            for (int i = 0; i < exits.Count; i++)
                if (cell.DistanceToSquared(exits[i].Position) < settings.entranceAvoidRadius * settings.entranceAvoidRadius)
                    return false;
            for (int i = 0; i < layout.AntChambers.Count; i++)
                if (cell.DistanceToSquared(layout.AntChambers[i].Nest) < settings.antNestAvoidRadius * settings.antNestAvoidRadius)
                    return false;
            return true;
        }

        //函数职责：利用少量已生成位置检查间距，不为每只蚁狮再次扫描整张地图。
        private static bool NearAny(IntVec3 cell, List<IntVec3> placed, float radius)
        {
            for (int i = 0; i < placed.Count; i++)
                if (cell.DistanceToSquared(placed[i]) < radius * radius) return true;
            return false;
        }
    }
}
