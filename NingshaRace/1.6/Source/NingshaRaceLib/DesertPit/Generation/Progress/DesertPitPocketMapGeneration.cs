using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

using NingshaRaceLib.DesertPit.Buildings;

namespace NingshaRaceLib.DesertPit.Generation.Progress
{
    //类职责：在主线程上分批生成沙漠巨坑口袋地图，并在安全阶段把执行权交还给当前场景窗口。
    internal static class DesertPitPocketMapGeneration
    {
        //字段职责：保存原版地图生成器清理临时网格与变量的方法入口。
        private static readonly MethodInfo ClearWorkingDataMethod = AccessTools.Method(typeof(MapGenerator), "ClearWorkingData");

        //函数职责：依次完成地图初始化、岩顶铺设、生成步骤和地图收尾，并在阶段间交还一帧。
        public static IEnumerable Generate(Building_DesertPitGate gate)
        {
            PocketMapParent parent = null;
            Map map = null;
            bool rockNoisesInitialized = false;
            bool mapAddedToGame = false;
            bool mapFinalized = false;
            bool completed = false;
            try
            {
                ClearWorkingData();
                MapGenerator.PlayerStartSpot = IntVec3.Invalid;
                MapGenerator.rootsToUnfog.Clear();
                MapGenerator.mapBeingGenerated = null;
                PocketMapUtility.currentlyGeneratingPortal = gate;

                MapGeneratorDef generator = gate.def.portal.pocketMapGenerator;
                int mapSize = gate.def.portal.pocketMapSize;
                int seed;
                DesertPitGenerationProgress.Report("准备地图", 0.02f);
                CreateMap(gate, generator, mapSize, out parent, out map, out seed);
                mapAddedToGame = Current.Game.Maps.Contains(map);
                if (!mapAddedToGame)
                {
                    throw new InvalidOperationException("沙漠巨坑基础地图未能加入当前游戏。");
                }
                yield return null;

                int roofedCells = 0;
                int totalCells = map.cellIndices.NumGridCells;
                foreach (IntVec3 cell in map.AllCells)
                {
                    map.roofGrid.SetRoof(cell, generator.roofDef ?? RoofDefOf.RoofRockThick);
                    roofedCells++;
                    if (roofedCells % 1536 == 0)
                    {
                        DesertPitGenerationProgress.Report("铺设岩顶", 0.02f + 0.06f * roofedCells / totalCells);
                        yield return null;
                    }
                }

                map.areaManager.AddStartingAreas();
                map.weatherDecider.StartInitialWeather();
                List<GenStepWithParams> genSteps = CollectGenSteps(generator, map);
                RockNoises.Init(map);
                rockNoisesInitialized = true;

                for (int i = 0; i < genSteps.Count; i++)
                {
                    float startProgress = 0.08f + 0.78f * i / genSteps.Count;
                    float endProgress = 0.08f + 0.78f * (i + 1) / genSteps.Count;
                    DesertPitGenerationProgress.SetStepRange(startProgress, endProgress);
                    DesertPitGenerationProgress.SetProgress(startProgress);
                    foreach (object unused in RunGenStep(genSteps, i, map, seed))
                    {
                        yield return null;
                    }

                    DesertPitGenerationProgress.SetProgress(endProgress);
                    yield return null;
                }

                DesertPitGenerationProgress.Report("初始化地图", 0.9f);
                Find.Scenario.PostMapGenerate(map);
                map.FinalizeInit();
                mapFinalized = true;
                yield return null;

                DesertPitGenerationProgress.Report("整理地图组件", 0.95f);
                MapComponentUtility.MapGenerated(map);
                parent.PostMapGenerate();
                MapGenerator.MapGeneratorPostInit(genSteps, map);
                Find.World.pocketMaps.Add(parent);
                gate.AssignPocketMap(map);
                completed = true;

                DesertPitGenerationProgress.Report("生成完成", 1f);
                yield return null;
            }
            finally
            {
                if (rockNoisesInitialized)
                {
                    RockNoises.Reset();
                }

                ClearWorkingData();
                MapGenerator.mapBeingGenerated = null;
                PocketMapUtility.currentlyGeneratingPortal = null;

                if (!completed && map != null)
                {
                    CleanupFailedMap(map, mapAddedToGame, mapFinalized);
                }
            }
        }

        //函数职责：创建口袋地图父对象与基础地图组件，并取得与原版一致的地图种子。
        private static void CreateMap(Building_DesertPitGate gate, MapGeneratorDef generator, int mapSize, out PocketMapParent parent, out Map map, out int seed)
        {
            parent = (PocketMapParent)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.PocketMap);
            parent.sourceMap = gate.Map;
            parent.mapGenerator = generator;
            seed = Gen.HashCombineInt(Find.World.info.Seed, parent.ID);

            Rand.PushState(seed);
            try
            {
                map = new Map();
                map.uniqueID = Find.UniqueIDsManager.GetNextMapID();
                map.generationTick = GenTicks.TicksGame;
                map.events = new MapEvents(map);
                map.info.Size = new IntVec3(mapSize, 1, mapSize);
                map.info.parent = parent;
                map.generatorDef = generator;
                map.info.disableSunShadows = generator.disableShadows;
                map.info.isPocketMap = true;
                map.pocketTileInfo = new Tile
                {
                    PrimaryBiome = generator.pocketMapProperties.biome
                };

                foreach (TileMutatorDef mutator in generator.pocketMapProperties.tileMutators)
                {
                    map.TileInfo.AddMutator(mutator);
                }

                map.ConstructComponents();
                foreach (TileMutatorDef mutator in map.TileInfo.Mutators)
                {
                    mutator.Worker?.Init(map);
                }

                MapGenerator.mapBeingGenerated = map;
                Current.Game.AddMap(map);
            }
            finally
            {
                Rand.PopState();
            }
        }

        //函数职责：按原版规则合并地图生成器、生物群系和地块变体提供的生成步骤。
        private static List<GenStepWithParams> CollectGenSteps(MapGeneratorDef generator, Map map)
        {
            IEnumerable<GenStepWithParams> steps = generator.genSteps
                .Where(IsScenarioAllowed)
                .Select((GenStepDef def) => new GenStepWithParams(def, default(GenStepParams)));

            foreach (TileMutatorDef mutator in map.TileInfo.Mutators)
            {
                steps = steps.Concat(mutator.extraGenSteps.Select((GenStepDef def) => new GenStepWithParams(def, default(GenStepParams))));
            }

            steps = steps.Concat(map.Biome.extraGenSteps.Where(IsScenarioAllowed).Select((GenStepDef def) => new GenStepWithParams(def, default(GenStepParams))));
            if (map.Biome.preventGenSteps.Any())
            {
                steps = steps.Where((GenStepWithParams step) => !map.Biome.preventGenSteps.Contains(step.def));
            }

            foreach (TileMutatorDef mutator in map.TileInfo.Mutators)
            {
                if (mutator.preventGenSteps.Any())
                {
                    steps = steps.Where((GenStepWithParams step) => !mutator.preventGenSteps.Contains(step.def));
                }
            }

            List<GenStepWithParams> result = steps.Distinct().OrderBy((GenStepWithParams step) => step.def.order).ThenBy((GenStepWithParams step) => step.def.index).ToList();
            result.RemoveAll((GenStepWithParams candidate) => result.Any((GenStepWithParams blocker) => blocker.def.preventsGenSteps != null && blocker.def.preventsGenSteps.Contains(candidate.def)));
            return result;
        }

        //函数职责：判断当前剧情设置是否允许指定地图生成步骤运行。
        private static bool IsScenarioAllowed(GenStepDef genStep)
        {
            return !Find.Scenario.AllParts.Any((ScenPart part) => typeof(ScenPart_DisableMapGen).IsAssignableFrom(part.def.scenPartClass) && part.def.genStep == genStep);
        }

        //函数职责：使用原版种子规则执行单个生成步骤，并保证随机状态不会跨越画面帧。
        private static IEnumerable RunGenStep(List<GenStepWithParams> genSteps, int index, Map map, int seed)
        {
            GenStepWithParams step = genSteps[index];
            int stepSeed = Gen.HashCombineInt(seed, GetSeedPart(genSteps, index));
            IDesertPitIncrementalGenStep incrementalStep = step.def.genStep as IDesertPitIncrementalGenStep;
            if (incrementalStep == null)
            {
                Rand.PushState(stepSeed);
                try
                {
                    step.def.genStep.Generate(map, step.parms);
                }
                finally
                {
                    Rand.PopState();
                }
            }
            else
            {
                IEnumerator incrementalEnumerator = incrementalStep.GenerateIncrementally(map, step.parms).GetEnumerator();
                DesertPitRandState randomState = DesertPitRandState.FromSeed(stepSeed);
                try
                {
                    while (MoveIncrementalStepNext(incrementalEnumerator, ref randomState))
                    {
                        yield return null;
                    }
                }
                finally
                {
                    IDisposable disposable = incrementalEnumerator as IDisposable;
                    disposable?.Dispose();
                }
            }

            if (map.pathing.IncrementalDirtyingDisabled)
            {
                map.pathing.ReEnableIncrementalDirtying();
                throw new InvalidOperationException("沙漠巨坑生成步骤结束后仍禁用了增量寻路更新：" + step.def.defName);
            }
        }

        //函数职责：在单个批次内恢复并保存地图生成随机流，确保随机状态栈在交还画面帧前已经平衡。
        private static bool MoveIncrementalStepNext(IEnumerator incrementalEnumerator, ref DesertPitRandState randomState)
        {
            Rand.PushState();
            try
            {
                randomState.Restore();
                bool hasMore = incrementalEnumerator.MoveNext();
                randomState = DesertPitRandState.Capture();
                return hasMore;
            }
            finally
            {
                Rand.PopState();
            }
        }

        //函数职责：计算重复生成步骤种子片段的序号偏移，保持与原版生成器一致。
        private static int GetSeedPart(List<GenStepWithParams> genSteps, int index)
        {
            int seedPart = genSteps[index].def.genStep.SeedPart;
            int duplicateCount = 0;
            for (int i = 0; i < index; i++)
            {
                if (genSteps[i].def.genStep.SeedPart == seedPart)
                {
                    duplicateCount++;
                }
            }

            return seedPart + duplicateCount;
        }

        //函数职责：调用原版私有清理流程释放地图生成期间创建的临时网格和共享变量。
        private static void ClearWorkingData()
        {
            ClearWorkingDataMethod.Invoke(null, null);
        }

        //函数职责：移除失败地图中的运行对象，并只对已完成绘制器初始化的地图执行完整释放。
        private static void CleanupFailedMap(Map map, bool mapAddedToGame, bool mapFinalized)
        {
            if (mapAddedToGame)
            {
                if (mapFinalized)
                {
                    Current.Game.DeinitAndRemoveMap(map, notifyPlayer: false);
                    return;
                }

                MapDeiniter.Deinit(map, notifyPlayer: false);
                Current.Game.Maps.Remove(map);
                Find.ColonistBar.MarkColonistsDirty();
                return;
            }

            if (mapFinalized)
            {
                map.Dispose();
            }
        }

    }
}
