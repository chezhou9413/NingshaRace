using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChezhouLib.CustomMission.MapTemplates;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.GiantTomb.Config;
using NingshaRaceLib.GiantTomb.Layout;
using NingshaRaceLib.GiantTomb.Metadata;
using NingshaRaceLib.PocketMaps.Generation;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation.Steps
{
    //类职责：加载全部墓葬模板并在受限随机回溯中建立200格口袋地图布局。
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
            if (map.Size.x != 200 || map.Size.z != 200)
            {
                throw new InvalidOperationException("巨型墓葬地图尺寸必须为200x200");
            }

            DesertPitGenerationProgress.SetStage("读取墓葬模板");
            Stopwatch templateTimer = Stopwatch.StartNew();
            int cacheHits = 0;
            List<GiantTombModule> required = new List<GiantTombModule>();
            for (int i = 0; i < layoutDef.modules.Count; i++)
            {
                required.Add(GiantTombMetadataLoader.Load(layoutDef.modules[i], out bool cacheHit));
                if (cacheHit) cacheHits++;
                DesertPitGenerationProgress.SetStepFraction(0.2f * (i + 1f) / layoutDef.modules.Count);
            }
            templateTimer.Stop();
            Log.Message("[NingshaRace] 巨型墓葬模板准备完成：" + required.Count + "个，缓存命中" + cacheHits + "个，耗时" + templateTimer.ElapsedMilliseconds + "毫秒。");
            yield return null;
            if (required.Count != 19 || required.Select((GiantTombModule module) => module.Def).Distinct().Count() != 19)
            {
                throw new InvalidOperationException("巨型墓葬必须配置19个互不重复的必选模板");
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
            int primaryAttemptCount = Math.Max(1, Math.Min(8, layoutDef.maxRestarts));
            GiantTombLayoutSearchAttempt[] primaryAttempts = BuildSearchAttempts(required, branchModules, leafModules,
                transitRoomModules, corridorModules, primaryAttemptCount, layoutDef.maxCandidateEvaluations, layoutDef.repeatCount);
            List<GiantTombModule> compactBranches = SelectSmallModules(branchModules, 2);
            List<GiantTombModule> compactLeaves = SelectSmallModules(leafModules, 4);
            List<GiantTombModule> compactTransitRooms = SelectSmallModules(transitRoomModules, 2);
            List<GiantTombModule> compactCorridors = SelectSmallModules(corridorModules, 2);
            const int rescueAttemptCount = 24;
            IntRange compactRepeatCount = new IntRange(layoutDef.repeatCount.min, layoutDef.repeatCount.min);
            GiantTombLayoutSearchAttempt[] rescueAttempts = BuildSearchAttempts(required, compactBranches, compactLeaves,
                compactTransitRooms, compactCorridors, rescueAttemptCount, rescueAttemptCount * 100000, compactRepeatCount);
            int totalAttemptCount = primaryAttempts.Length + rescueAttempts.Length;
            GiantTombParallelLayoutSearch search = new GiantTombParallelLayoutSearch(primaryAttempts, entrance, terminalModules,
                map.Size.x, map.Size.z, layoutDef.borderMargin);
            DesertPitGenerationProgress.SetStage("多线程拼接墓葬结构 0/" + totalAttemptCount);
            search.Start();
            while (!search.Completion.IsCompleted)
            {
                int completed = search.CompletedAttempts;
                DesertPitGenerationProgress.SetStage("多线程拼接墓葬结构 " + completed + "/" + totalAttemptCount);
                DesertPitGenerationProgress.SetStepFraction(0.2f + 0.8f * completed / totalAttemptCount);
                yield return null;
            }

            GiantTombLayoutSearchResult searchResult = search.Completion.GetAwaiter().GetResult();
            int usedRescueRound = 0;
            if (!searchResult.Success)
            {
                long accumulatedEvaluations = searchResult.TotalEvaluations;
                long accumulatedMilliseconds = searchResult.ElapsedMilliseconds;
                int deepestPlacementCount = searchResult.DeepestPlacementCount;
                int rescueRound = 0;
                while (!searchResult.Success)
                {
                    rescueRound++;
                    usedRescueRound = rescueRound;
                    if (rescueRound > 1)
                    {
                        rescueAttempts = BuildSearchAttempts(required, compactBranches, compactLeaves,
                            compactTransitRooms, compactCorridors, rescueAttemptCount, rescueAttemptCount * 100000, compactRepeatCount);
                    }
                    search = new GiantTombParallelLayoutSearch(rescueAttempts, entrance, terminalModules,
                        map.Size.x, map.Size.z, layoutDef.borderMargin);
                    search.Start();
                    while (!search.Completion.IsCompleted)
                    {
                        int completed = search.CompletedAttempts;
                        DesertPitGenerationProgress.SetStage("保底拼接墓葬结构 第" + rescueRound + "轮 " + completed + "/" + rescueAttempts.Length);
                        DesertPitGenerationProgress.SetStepFraction(0.45f + 0.5f * completed / rescueAttempts.Length);
                        yield return null;
                    }
                    searchResult = search.Completion.GetAwaiter().GetResult();
                    accumulatedEvaluations += searchResult.TotalEvaluations;
                    accumulatedMilliseconds += searchResult.ElapsedMilliseconds;
                    deepestPlacementCount = Math.Max(deepestPlacementCount, searchResult.DeepestPlacementCount);
                }
                searchResult.TotalEvaluations = accumulatedEvaluations;
                searchResult.ElapsedMilliseconds = accumulatedMilliseconds;
                searchResult.DeepestPlacementCount = deepestPlacementCount;
            }

            GiantTombLayoutData data = BuildLayoutData(map, searchResult.Placements, searchResult.Connections, entrance);
            GiantTombGenUtility.SetLayoutData(data);
            GiantTombLayoutSearchAttempt selectedAttempt = searchResult.Attempt;
            string searchMode = usedRescueRound == 0 ? "正常阶段" : "保底第" + usedRescueRound + "轮";
            Log.Message("[NingshaRace] 巨型墓葬并行布局完成：" + searchMode + "采用尝试" + selectedAttempt.Index + "，检查候选" + searchResult.TotalEvaluations
                + "个，耗时" + searchResult.ElapsedMilliseconds + "毫秒。");
            Log.Message("[NingshaRace] 巨型墓葬额外模块：分支中转" + selectedAttempt.BranchCount + "，尽头/小房间" + selectedAttempt.LeafCount
                + "，二接口中转房" + selectedAttempt.TransitRoomCount + "，普通走廊" + selectedAttempt.CorridorCount + "。");
            DesertPitGenerationProgress.SetStepFraction(1f);
        }

        //函数职责：按模板矩形面积选择指定数量的小型模块，供保底布局降低空间拥挤。
        private static List<GiantTombModule> SelectSmallModules(List<GiantTombModule> modules, int count)
        {
            return modules.OrderBy((GiantTombModule module) => module.Width * module.Height)
                .ThenBy((GiantTombModule module) => module.Def.defName).Take(count).ToList();
        }

        //函数职责：在主线程把终点Def引用解析为稳定模块数组，避免后台访问Def配置集合。
        private static GiantTombModule[] ResolveTerminalModules(List<GiantTombModule> required, List<ClMapTemplateDef> definitions)
        {
            GiantTombModule[] result = new GiantTombModule[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                GiantTombModule module = required.FirstOrDefault((GiantTombModule candidate) => candidate.Def == definitions[i]);
                if (module == null) throw new InvalidOperationException("巨型墓葬终点模板未加载: " + definitions[i]?.defName);
                result[i] = module;
            }
            return result;
        }

        //函数职责：在主线程预先冻结全部模块池、预算和局部随机种子，作为后台并行搜索的唯一输入。
        private static GiantTombLayoutSearchAttempt[] BuildSearchAttempts(List<GiantTombModule> required, List<GiantTombModule> branches,
            List<GiantTombModule> leaves, List<GiantTombModule> transitRooms, List<GiantTombModule> corridors,
            int maximumAttempts, int totalCandidateBudget, IntRange repeatCountRange)
        {
            int perAttemptBudget = Math.Max(1000, totalCandidateBudget / maximumAttempts);
            int remainingBudget = totalCandidateBudget;
            List<GiantTombLayoutSearchAttempt> result = new List<GiantTombLayoutSearchAttempt>(maximumAttempts);
            for (int index = 0; index < maximumAttempts && remainingBudget > 0; index++)
            {
                int repeatCount = repeatCountRange.RandomInRange;
                List<GiantTombModule> pool = BuildPool(required, branches, leaves, transitRooms, corridors, repeatCount, out RepeatPoolCounts counts);
                ValidateDegreeInvariant(pool);
                int budget = Math.Min(perAttemptBudget, remainingBudget);
                result.Add(new GiantTombLayoutSearchAttempt(index, pool.ToArray(), Rand.Int, budget,
                    counts.Branches, counts.Leaves, counts.TransitRooms, counts.Corridors));
                remainingBudget -= budget;
            }
            if (result.Count == 0) throw new InvalidOperationException("巨型墓葬布局没有可执行的搜索预算");
            return result.ToArray();
        }

        //函数职责：把配置Def列表解析为已经加载的模块并验证该类别要求的连接点数量。
        private static List<GiantTombModule> ResolveRepeatModules(List<GiantTombModule> required, List<ClMapTemplateDef> definitions, int connectorCount, string category)
        {
            List<GiantTombModule> result = new List<GiantTombModule>();
            for (int i = 0; i < definitions.Count; i++)
            {
                GiantTombModule module = required.FirstOrDefault((GiantTombModule candidate) => candidate.Def == definitions[i]);
                if (module == null || module.Connectors.Count != connectorCount)
                {
                    throw new InvalidOperationException("巨型墓葬" + category + "必须使用" + connectorCount + "连接点模板: " + definitions[i]?.defName);
                }
                result.Add(module);
            }
            return result;
        }

        //函数职责：按接口守恒比例组合分支、尽头、中转房和少量走廊，使新增模块仍能构成完整树。
        private static List<GiantTombModule> BuildPool(List<GiantTombModule> required, List<GiantTombModule> branches, List<GiantTombModule> leaves, List<GiantTombModule> transitRooms, List<GiantTombModule> corridors, int repeatCount, out RepeatPoolCounts counts)
        {
            int branchCount = Math.Max(1, (repeatCount + 2) / 5);
            while (branchCount * 3 > repeatCount) branchCount--;
            int leafCount = branchCount * 2;
            int degreeTwoCount = repeatCount - branchCount - leafCount;
            int transitRoomCount = Math.Min(degreeTwoCount, Math.Max(1, (degreeTwoCount * 3 + 2) / 4));
            int corridorCount = degreeTwoCount - transitRoomCount;
            counts = new RepeatPoolCounts(branchCount, leafCount, transitRoomCount, corridorCount);

            for (int attempt = 0; attempt < 32; attempt++)
            {
                List<GiantTombModule> result = new List<GiantTombModule>(required.Count + repeatCount);
                result.AddRange(required);
                AddWeightedWithDiversity(result, branches, branchCount);
                AddWeightedWithDiversity(result, leaves, leafCount);
                AddWeightedWithDiversity(result, transitRooms, transitRoomCount);
                AddWeightedWithDiversity(result, corridors, corridorCount);
                if (GiantTombConnectorCompatibility.HasEvenConnectorComponents(result)) return result;
            }
            throw new InvalidOperationException("巨型墓葬随机模板池无法满足各接口兼容组的偶数闭合条件");
        }

        //函数职责：先尽量覆盖类别中的不同模板，再按Def权重补足该类别的重复数量。
        private static void AddWeightedWithDiversity(List<GiantTombModule> target, List<GiantTombModule> choices, int count)
        {
            if (count <= 0) return;
            List<GiantTombModule> shuffled = choices.InRandomOrder().ToList();
            int diverseCount = Math.Min(count, shuffled.Count);
            for (int i = 0; i < diverseCount; i++) target.Add(shuffled[i]);
            for (int i = diverseCount; i < count; i++)
            {
                target.Add(choices.RandomElementByWeight((GiantTombModule module) => module.Def.selectionWeight));
            }
        }

        //函数职责：验证所有连接点数量恰好能构成一个使用全部接口的树。
        private static void ValidateDegreeInvariant(List<GiantTombModule> pool)
        {
            int connectorCount = pool.Sum((GiantTombModule module) => module.Connectors.Count);
            int expected = 2 * (pool.Count - 1);
            if (connectorCount != expected)
            {
                throw new InvalidOperationException("巨型墓葬连接点总数不能构成完整树: " + connectorCount + " != " + expected);
            }
            if (!GiantTombConnectorCompatibility.HasEvenConnectorComponents(pool))
            {
                throw new InvalidOperationException("巨型墓葬接口兼容组存在奇数个连接点，无法全部闭合");
            }
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

        //结构职责：保存一次额外模块池中各探索类型的实际数量，供日志和布局结果核对。
        private readonly struct RepeatPoolCounts
        {
            public readonly int Branches;
            public readonly int Leaves;
            public readonly int TransitRooms;
            public readonly int Corridors;

            //函数职责：记录一次满足接口守恒的分类抽取数量。
            public RepeatPoolCounts(int branches, int leaves, int transitRooms, int corridors)
            {
                Branches = branches;
                Leaves = leaves;
                TransitRooms = transitRooms;
                Corridors = corridors;
            }
        }
    }
}
