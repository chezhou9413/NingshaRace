using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static NingshaRaceLib.GiantTomb.Generation.GiantTombSearchPoolBuilder;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.GiantTomb.Config;
using NingshaRaceLib.GiantTomb.Layout;
using NingshaRaceLib.GiantTomb.Metadata;
using NingshaRaceLib.PocketMaps.Generation;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation.Steps
{
    //类职责：加载配置指定的墓葬模板并在受限随机回溯中建立对应尺寸的地下布局。
    public sealed class GenStep_GiantTombLayout : GenStep, INingshaIncrementalGenStep
    {
        //字段职责：引用本生成步骤使用的模板清单和搜索限制。
        public NingshaGiantTombLayoutDef layoutDef;

        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 197428311;

        //函数职责：兼容原版同步生成入口并完整执行分批布局流程。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：批量准备缓存模板并按重启预算求解完整树形墓葬布局。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            if (layoutDef == null)
            {
                throw new InvalidOperationException("巨型墓葬生成步骤缺少layoutDef");
            }
            if (map.Size.x != layoutDef.requiredMapSize || map.Size.z != layoutDef.requiredMapSize)
            {
                throw new InvalidOperationException("墓葬地图尺寸必须为" + layoutDef.requiredMapSize + "x" + layoutDef.requiredMapSize);
            }

            DesertPitGenerationProgress.SetStage("勘察墓葬");
            Stopwatch templateTimer = Stopwatch.StartNew();
            int cacheHits = 0;
            List<GiantTombModule> required = new List<GiantTombModule>();
            for (int i = 0; i < layoutDef.modules.Count; i++)
            {
                required.Add(GiantTombMetadataLoader.Load(layoutDef.modules[i], out bool cacheHit));
                if (cacheHit) cacheHits++;
                DesertPitGenerationProgress.SetStepFraction(0.2f * (i + 1f) / layoutDef.modules.Count);
                //冷缓存读盘后交还生成驱动，允许它依据本帧预算刷新界面。
                if (!cacheHit)
                {
                    templateTimer.Stop();
                    yield return null;
                    templateTimer.Start();
                }
            }
            templateTimer.Stop();
            Log.Message("[NingshaRace] 巨型墓葬模板准备完成：" + required.Count + "个，缓存命中" + cacheHits + "个，耗时" + templateTimer.ElapsedMilliseconds + "毫秒。");
            yield return null;
            if (required.Count != layoutDef.modules.Count || required.Select((GiantTombModule module) => module.Def).Distinct().Count() != layoutDef.modules.Count)
            {
                throw new InvalidOperationException("墓葬布局必须完整加载配置中全部互不重复的必选模板");
            }
            GiantTombModule entrance = required.FirstOrDefault((GiantTombModule module) => module.Def == layoutDef.entranceTemplate);
            if (entrance == null)
            {
                throw new InvalidOperationException("巨型墓葬入口模板未加载");
            }
            List<GiantTombModule> branchModules = ResolveRepeatModules(required, layoutDef.repeatBranchTemplates, 4, "四接口中转节点");
            List<GiantTombModule> leafModules = ResolveRepeatModules(required, layoutDef.repeatLeafTemplates, 1, "尽头和小房间");
            List<GiantTombModule> transitRoomModules = ResolveRepeatModules(required, layoutDef.repeatTransitRoomTemplates, 2, "二接口中转房间");
            List<GiantTombModule> corridorModules = ResolveRepeatModules(required, layoutDef.repeatCorridorTemplates, 2, "二接口走廊");

            GiantTombModule[] terminalModules = ResolveTerminalModules(required, layoutDef.terminalTemplates);
            GiantTombSearchCatalog catalog = new GiantTombSearchCatalog(required);
            int primaryAttemptCount = Math.Max(1, Math.Min(8, layoutDef.maxRestarts));
            GiantTombLayoutSearchAttempt[] primaryAttempts = BuildSearchAttempts(required, branchModules, leafModules,
                transitRoomModules, corridorModules, primaryAttemptCount, layoutDef.maxCandidateEvaluations, layoutDef.repeatCount);
            GiantTombLayoutSearchResult searchResult;
            using (GiantTombParallelLayoutSearch search = new GiantTombParallelLayoutSearch(primaryAttempts, entrance, catalog,
                terminalModules, map.Size.x, map.Size.z, layoutDef.borderMargin))
            {
                search.Start();
                while (!search.Completion.IsCompleted)
                {
                    DesertPitGenerationProgress.SetStage("寻找墓葬中的通路");
                    DesertPitGenerationProgress.SetStepFraction(0.2f + 0.35f * search.CompletedAttempts / primaryAttempts.Length);
                    yield return null;
                }
                searchResult = search.Completion.GetAwaiter().GetResult();
            }

            int compactAttemptCount = Math.Min(24, layoutDef.maxRestarts - primaryAttempts.Length);
            bool usedCompact = !searchResult.Success && compactAttemptCount > 0;
            if (usedCompact)
            {
                //只有正常阶段确实失败才抽取紧凑池，避免成功路径提前构造和随机抽取二十四组备用方案。
                GiantTombLayoutSearchAttempt[] compactAttempts = BuildSearchAttempts(required,
                    SelectSmallModules(branchModules, 2), SelectSmallModules(leafModules, 4),
                    SelectSmallModules(transitRoomModules, 2), SelectSmallModules(corridorModules, 2),
                    compactAttemptCount, layoutDef.maxCompactCandidateEvaluations,
                    new IntRange(layoutDef.repeatCount.min, layoutDef.repeatCount.min));
                GiantTombLayoutSearchResult normalResult = searchResult;
                using (GiantTombParallelLayoutSearch search = new GiantTombParallelLayoutSearch(compactAttempts, entrance, catalog,
                    terminalModules, map.Size.x, map.Size.z, layoutDef.borderMargin))
                {
                    search.Start();
                    while (!search.Completion.IsCompleted)
                    {
                        DesertPitGenerationProgress.SetStage("寻找更合适的通路");
                        DesertPitGenerationProgress.SetStepFraction(0.55f + 0.4f * search.CompletedAttempts / compactAttempts.Length);
                        yield return null;
                    }
                    searchResult = search.Completion.GetAwaiter().GetResult();
                }
                searchResult.TotalEvaluations += normalResult.TotalEvaluations;
                searchResult.CollisionChecks += normalResult.CollisionChecks;
                searchResult.CompletedAttempts += normalResult.CompletedAttempts;
                searchResult.ElapsedMilliseconds += normalResult.ElapsedMilliseconds;
                searchResult.DeepestPlacementCount = Math.Max(searchResult.DeepestPlacementCount, normalResult.DeepestPlacementCount);
            }
            if (!searchResult.Success)
            {
                throw new InvalidOperationException("墓葬布局搜索预算耗尽：" + layoutDef.defName + "，尝试"
                    + searchResult.CompletedAttempts + "次，摆放候选" + searchResult.TotalEvaluations
                    + "个，矩形碰撞检查" + searchResult.CollisionChecks + "次，最深"
                    + searchResult.DeepestPlacementCount + "个模块，耗时" + searchResult.ElapsedMilliseconds
                    + "毫秒。请检查模板空间和布局预算；不会生成不完整墓葬。");
            }

            GiantTombLayoutData data = BuildLayoutData(map, searchResult.Placements, searchResult.Connections, entrance);
            GiantTombGenUtility.SetLayoutData(data);
            GiantTombLayoutSearchAttempt selectedAttempt = searchResult.Attempt;
            string searchMode = usedCompact ? "紧凑阶段" : "正常阶段";
            Log.Message("[NingshaRace] 巨型墓葬并行布局完成：" + searchMode + "采用尝试" + selectedAttempt.Index + "，检查候选" + searchResult.TotalEvaluations
                + "个，完成尝试" + searchResult.CompletedAttempts + "次，矩形碰撞检查" + searchResult.CollisionChecks + "次，耗时" + searchResult.ElapsedMilliseconds + "毫秒。");
            Log.Message("[NingshaRace] 巨型墓葬额外模块：分支中转" + selectedAttempt.BranchCount + "，尽头/小房间" + selectedAttempt.LeafCount
                + "，二接口中转房" + selectedAttempt.TransitRoomCount + "，普通走廊" + selectedAttempt.CorridorCount + "。");
            DesertPitGenerationProgress.SetStepFraction(1f);
        }

        //函数职责：把求解结果转换为格网掩码并登记给后续地形、模板和矿物步骤。
        private static GiantTombLayoutData BuildLayoutData(Map map, List<GiantTombPlacement> placements, List<GiantTombConnection> connections, GiantTombModule entrance)
        {
            GiantTombLayoutData data = new GiantTombLayoutData
            {
                StructureCells = new BitArray(map.cellIndices.NumGridCells)
            };
            data.Placements.AddRange(placements);
            data.Connections.AddRange(connections);
            data.Entrance = placements.First((GiantTombPlacement placement) => placement.Module == entrance && placement.InstanceId == 0);
            for (int i = 0; i < placements.Count; i++)
            {
                GiantTombPlacement placement = placements[i];
                MapGenerator.UsedRects.Add(placement.Bounds);
                foreach (IntVec3 cell in GiantTombTransformUtility.StructureCells(placement))
                {
                    if (!cell.InBounds(map))
                    {
                        throw new InvalidOperationException("巨型墓葬结构格超出地图: " + placement.Module.Def.defName);
                    }
                    int index = map.cellIndices.CellToIndex(cell);
                    data.StructureCells[index] = true;
                    MapGenerator.Caves[cell] = 1f;
                }
            }
            return data;
        }

    }
}
