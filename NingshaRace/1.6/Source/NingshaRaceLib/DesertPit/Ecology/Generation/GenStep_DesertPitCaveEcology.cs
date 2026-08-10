using System.Collections;
using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Ecology.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Ecology.Generation
{
    //类职责：在蚁巢生成后分帧放置一个原版活跃虫巢与五至八只固定洞穴动物。
    public class GenStep_DesertPitCaveEcology : GenStep, IDesertPitIncrementalGenStep
    {
        private const int Seed = 604238179;

        public override int SeedPart => Seed;

        //函数职责：在原版同步地图生成入口中完整执行洞穴生态生成迭代器。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：分批筛选虫巢和动物候选格，并在每个实体生成后主动交还当前场景更新帧。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            DefModExtension_DesertPitFauna settings = DefOfRefs.NingshaRace_DesertPitBiome.GetModExtension<DefModExtension_DesertPitFauna>();
            if (Find.Storyteller.difficulty.allowCaveHives && Faction.OfInsects != null && settings.hiveCount > 0)
            {
                foreach (object unused in GenerateHive(map, data, settings))
                {
                    yield return null;
                }
            }

            foreach (object unused in GenerateAnimals(map, data, settings))
            {
                yield return null;
            }
        }

        //函数职责：分帧扫描合法侧洞，并生成允许补虫但禁止扩张的单个原版虫巢。
        private static IEnumerable GenerateHive(Map map, DesertPitLayoutData data, DefModExtension_DesertPitFauna settings)
        {
            DesertPitGenUtility.SetGenerationStatus("原版虫巢：筛选侧洞");
            List<IntVec3> candidates = new List<IntVec3>();
            int scanned = 0;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (DesertPitCaveEcologyUtility.CanPlaceHiveAt(map, data, settings, cell))
                {
                    candidates.Add(cell);
                }

                scanned++;
                if (scanned % settings.scanBatchSize == 0)
                {
                    yield return null;
                }
            }

            if (candidates.Count == 0)
            {
                throw new System.InvalidOperationException("沙漠巨坑没有找到可生成原版虫巢的干燥侧洞。");
            }

            IntVec3 hiveCell = candidates.RandomElementByWeight(delegate(IntVec3 cell)
            {
                return DesertPitCaveEcologyUtility.HivePlacementWeight(map, data, cell);
            });
            DesertPitCaveEcologyUtility.ReserveScene(map, data, hiveCell, settings.hiveSceneRadius);
            DesertPitGenUtility.SetGenerationStatus("原版虫巢：唤醒虫群");
            HiveUtility.SpawnHive(
                hiveCell,
                map,
                WipeMode.VanishOrMoveAside,
                spawnInsectsImmediately: true,
                canSpawnHives: false,
                canSpawnInsects: true,
                dormant: false,
                aggressive: true);
            yield return null;
        }

        //函数职责：生成固定物种池的成年野生动物，并通过格子移除保证个体在洞穴中分散出现。
        private static IEnumerable GenerateAnimals(Map map, DesertPitLayoutData data, DefModExtension_DesertPitFauna settings)
        {
            DesertPitGenUtility.SetGenerationStatus("洞穴动物：筛选栖息地");
            List<IntVec3> candidates = new List<IntVec3>();
            int scanned = 0;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (DesertPitCaveEcologyUtility.CanPlaceAnimalAt(map, data, settings, cell))
                {
                    candidates.Add(cell);
                }

                scanned++;
                if (scanned % settings.scanBatchSize == 0)
                {
                    yield return null;
                }
            }

            List<PawnKindDef> kinds = DesertPitCaveEcologyUtility.BuildAnimalKinds(settings);
            for (int i = 0; i < kinds.Count; i++)
            {
                IntVec3 spawnCell;
                if (!candidates.TryRandomElement(out spawnCell))
                {
                    throw new System.InvalidOperationException("沙漠巨坑没有足够的分散格子生成配置数量的洞穴动物。");
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kinds[i]);
                GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
                RemoveNearbyCandidates(candidates, spawnCell, settings.animalSpacing);
                DesertPitGenUtility.SetGenerationStatus("洞穴动物：" + (i + 1) + "/" + kinds.Count);
                yield return null;
            }
        }

        //函数职责：移除已生成动物周围的候选格，使后续动物保持自然分散的最小距离。
        private static void RemoveNearbyCandidates(List<IntVec3> candidates, IntVec3 center, float radius)
        {
            float squaredRadius = radius * radius;
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].DistanceToSquared(center) < squaredRadius)
                {
                    candidates.RemoveAt(i);
                }
            }
        }
    }
}
