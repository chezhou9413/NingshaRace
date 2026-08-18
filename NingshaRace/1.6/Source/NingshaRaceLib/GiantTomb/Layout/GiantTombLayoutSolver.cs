using System;
using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：使用受限随机回溯把全部墓葬模块连接成无悬空接口的纵深树形结构。
    internal sealed class GiantTombLayoutSolver
    {
        private readonly int mapWidth;
        private readonly int mapHeight;
        private readonly int borderMargin;
        private readonly GiantTombModule[] terminalModules;
        private readonly GiantTombLayoutRandom random;
        private readonly Dictionary<GiantTombModule, List<PlacementVariant>[]> variantsByModule = new Dictionary<GiantTombModule, List<PlacementVariant>[]>();
        private readonly HashSet<GiantTombModule> visitedModules = new HashSet<GiantTombModule>();
        private readonly HashSet<int> reachableSignatures = new HashSet<int>();
        private readonly Func<bool> shouldStop;
        private GiantTombPlacementSpatialIndex spatialIndex;
        private bool[] reachedBuffer;
        private int evaluations;
        private int evaluationLimit;
        private int deepestPlacementCount;

        public int Evaluations => evaluations;
        public int DeepestPlacementCount => deepestPlacementCount;

        //函数职责：建立一个使用指定地图尺寸和配置的布局求解器。
        public GiantTombLayoutSolver(int mapWidth, int mapHeight, int borderMargin, GiantTombModule[] terminalModules, int randomSeed, Func<bool> shouldStop = null)
        {
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
            this.borderMargin = borderMargin;
            this.terminalModules = terminalModules ?? throw new ArgumentNullException(nameof(terminalModules));
            this.shouldStop = shouldStop;
            random = new GiantTombLayoutRandom(randomSeed);
        }

        //函数职责：在单次候选预算内尝试求出一套完整布局。
        public bool TrySolve(IReadOnlyList<GiantTombModule> pool, GiantTombModule entrance, int candidateBudget, out List<GiantTombPlacement> placements, out List<GiantTombConnection> connections)
        {
            evaluations = 0;
            evaluationLimit = candidateBudget;
            deepestPlacementCount = 1;
            PreparePlacementVariants(pool);
            placements = new List<GiantTombPlacement>();
            connections = new List<GiantTombConnection>();
            List<ModuleInstance> remaining = BuildInstances(pool, entrance);
            GiantTombPlacement root = BuildRoot(entrance);
            placements.Add(root);
            spatialIndex = new GiantTombPlacementSpatialIndex(mapWidth, mapHeight, pool.Count + 1);
            spatialIndex.Add(root);
            reachedBuffer = new bool[pool.Count];
            if (!Search(remaining, placements, connections))
            {
                placements = null;
                connections = null;
                return false;
            }
            return true;
        }

        //函数职责：为每个模块实例分配稳定编号并移除已经作为根节点使用的入口实例。
        private static List<ModuleInstance> BuildInstances(IReadOnlyList<GiantTombModule> pool, GiantTombModule entrance)
        {
            List<ModuleInstance> result = new List<ModuleInstance>();
            bool removedEntrance = false;
            for (int i = 0; i < pool.Count; i++)
            {
                if (!removedEntrance && pool[i] == entrance)
                {
                    removedEntrance = true;
                    continue;
                }
                result.Add(new ModuleInstance { Id = i + 1, Module = pool[i] });
            }
            return result;
        }

        //函数职责：把单连接点入口朝北并放置在地图南部中央。
        private GiantTombPlacement BuildRoot(GiantTombModule entrance)
        {
            if (entrance.Connectors.Count != 1)
            {
                throw new InvalidOperationException("巨型墓葬入口模板必须恰好有一个连接点");
            }
            List<ClMapTransform> transforms = new List<ClMapTransform>();
            foreach (ClMapTransform transform in EnumerateTransforms(entrance))
            {
                if (transform.TransformRotation(entrance.Connectors[0].Direction) == Rot4.North) transforms.Add(transform);
            }
            random.Shuffle(transforms);
            if (transforms.Count == 0)
            {
                throw new InvalidOperationException("巨型墓葬入口模板无法朝北放置");
            }
            ClMapTransform selected = transforms[0];
            IntVec2 size = selected.GetOutputSize(entrance.Width, entrance.Height);
            int jitter = random.RangeInclusive(-4, 4);
            int x = (mapWidth - size.x) / 2 + jitter;
            IntVec3 origin = new IntVec3(x, 0, borderMargin);
            return GiantTombTransformUtility.BuildPlacement(entrance, origin, selected, 0, 0);
        }

        //函数职责：递归选择约束最强的开放接口并尝试兼容模块、方向和空间位置。
        private bool Search(List<ModuleInstance> remaining, List<GiantTombPlacement> placements, List<GiantTombConnection> connections)
        {
            if (shouldStop != null && shouldStop()) return false;
            if (placements.Count > deepestPlacementCount) deepestPlacementCount = placements.Count;
            if (remaining.Count == 0)
            {
                for (int i = 0; i < placements.Count; i++)
                {
                    for (int j = 0; j < placements[i].Connectors.Count; j++)
                    {
                        if (!placements[i].Connectors[j].Connected) return false;
                    }
                }
                return true;
            }
            if (evaluations >= evaluationLimit)
            {
                return false;
            }
            if (!CanStillConnectRemaining(remaining, placements))
            {
                return false;
            }
            FrontierChoice choice = SelectFrontierChoice(remaining, placements);
            if (choice == null || choice.Candidates.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < choice.Candidates.Count && evaluations < evaluationLimit; i++)
            {
                if (shouldStop != null && shouldStop()) return false;
                Candidate candidate = choice.Candidates[i];
                evaluations++;
                GiantTombPlacement child = candidate.Variant.Prototype.Build(candidate.Instance.Module,
                    candidate.Origin, candidate.Instance.Id, choice.Parent.Depth + 1);
                GiantTombPlacedConnector childConnector = child.Connectors[candidate.Variant.ConnectorIndex];
                choice.Frontier.Connected = true;
                childConnector.Connected = true;
                placements.Add(child);
                spatialIndex.Add(child);
                connections.Add(new GiantTombConnection
                {
                    Parent = choice.Parent,
                    ParentConnector = choice.Frontier,
                    Child = child,
                    ChildConnector = childConnector
                });
                remaining.Remove(candidate.Instance);
                if (Search(remaining, placements, connections))
                {
                    return true;
                }
                remaining.Add(candidate.Instance);
                connections.RemoveAt(connections.Count - 1);
                spatialIndex.Remove(child);
                placements.RemoveAt(placements.Count - 1);
                childConnector.Connected = false;
                choice.Frontier.Connected = false;
            }
            return false;
        }

        //函数职责：先用预计算接口签名低成本选择约束最强的出口，再只为该出口构造真实空间候选。
        private FrontierChoice SelectFrontierChoice(List<ModuleInstance> remaining, List<GiantTombPlacement> placements)
        {
            FrontierChoice selected = null;
            int selectedOptionCount = int.MaxValue;
            foreach (GiantTombPlacement placement in placements)
            {
                foreach (GiantTombPlacedConnector connector in placement.Connectors)
                {
                    if (connector.Connected) continue;
                    int optionCount = CountSpatialVariants(connector, placement, remaining, selectedOptionCount);
                    if (optionCount == 0)
                    {
                        return new FrontierChoice { Parent = placement, Frontier = connector, Candidates = new List<Candidate>() };
                    }
                    if (selected == null || optionCount < selectedOptionCount || optionCount == selectedOptionCount && random.Bool())
                    {
                        selectedOptionCount = optionCount;
                        selected = new FrontierChoice { Parent = placement, Frontier = connector };
                    }
                }
            }
            if (selected != null)
            {
                selected.Candidates = BuildCandidates(selected.Frontier, selected.Parent, remaining);
            }
            return selected;
        }

        //函数职责：以边界矩形快速统计出口的真实可摆放变换，不创建完整模块或连接点坐标列表。
        private int CountSpatialVariants(GiantTombPlacedConnector frontier, GiantTombPlacement parent, List<ModuleInstance> remaining, int stopAfter)
        {
            int count = 0;
            visitedModules.Clear();
            for (int i = 0; i < remaining.Count; i++)
            {
                GiantTombModule module = remaining[i].Module;
                if (!visitedModules.Add(module)) continue;
                List<PlacementVariant> variants = variantsByModule[module][frontier.Direction.Opposite.AsInt];
                for (int j = 0; j < variants.Count; j++)
                {
                    PlacementVariant variant = variants[j];
                    GiantTombConnector source = module.Connectors[variant.ConnectorIndex];
                    if (GiantTombConnectorCompatibility.AreCompatible(frontier.Kind, frontier.Cells.Count, source.Kind, source.Cells.Count))
                    {
                        IntVec3 origin = frontier.AlignmentCell + frontier.Direction.FacingCell - variant.AlignmentCell;
                        CellRect bounds = new CellRect(origin.x, origin.z, variant.Size.x, variant.Size.z);
                        if (IsBoundsValid(bounds, parent)) count++;
                        if (count > stopAfter) return count;
                    }
                }
            }
            return count;
        }

        //函数职责：枚举与一个父接口兼容且满足地图边界、间隔和纵深偏好的全部子模块候选。
        private List<Candidate> BuildCandidates(GiantTombPlacedConnector frontier, GiantTombPlacement parent, List<ModuleInstance> remaining)
        {
            List<Candidate> result = new List<Candidate>();
            visitedModules.Clear();
            for (int instanceIndex = 0; instanceIndex < remaining.Count; instanceIndex++)
            {
                ModuleInstance instance = remaining[instanceIndex];
                GiantTombModule module = instance.Module;
                if (!visitedModules.Add(module)) continue;
                List<PlacementVariant> variants = variantsByModule[module][frontier.Direction.Opposite.AsInt];
                for (int variantIndex = 0; variantIndex < variants.Count; variantIndex++)
                {
                    PlacementVariant variant = variants[variantIndex];
                    GiantTombConnector source = module.Connectors[variant.ConnectorIndex];
                    if (!GiantTombConnectorCompatibility.AreCompatible(frontier.Kind, frontier.Cells.Count, source.Kind, source.Cells.Count)) continue;
                    IntVec3 origin = frontier.AlignmentCell + frontier.Direction.FacingCell - variant.AlignmentCell;
                    CellRect bounds = new CellRect(origin.x, origin.z, variant.Size.x, variant.Size.z);
                    if (!IsBoundsValid(bounds, parent)) continue;
                    float score = ScoreCandidate(bounds, parent.Depth + 1, module, remaining.Count);
                    result.Add(new Candidate { Instance = instance, Origin = origin, Variant = variant, Score = score });
                }
            }
            result.Sort((Candidate left, Candidate right) => right.Score.CompareTo(left.Score));
            if (result.Count > 256) result.RemoveRange(256, result.Count - 256);
            return result;
        }

        //函数职责：按纵深、分支能力、横向居中和地图边界余量排序真实可摆放候选。
        private float ScoreCandidate(CellRect bounds, int depth, GiantTombModule module, int remainingCount)
        {
            int borderClearance = Math.Min(Math.Min(bounds.minX - borderMargin, bounds.minZ - borderMargin),
                Math.Min(mapWidth - borderMargin - 1 - bounds.maxX, mapHeight - borderMargin - 1 - bounds.maxZ));
            int area = bounds.Width * bounds.Height;
            float largeModulePriority = remainingCount > 12 ? area * 0.45f : area * 0.1f;
            float score = bounds.maxZ * 0.75f + module.Connectors.Count * 90f + borderClearance * 1.5f + largeModulePriority + random.Value();
            if (IsTerminal(module))
            {
                score += depth * 60f;
                if (remainingCount > 16) score -= 300f;
            }
            return score;
        }

        //函数职责：在主线程准备的终点模块快照中判断当前模块是否属于终点大墓室。
        private bool IsTerminal(GiantTombModule module)
        {
            for (int i = 0; i < terminalModules.Length; i++)
            {
                if (terminalModules[i] == module) return true;
            }
            return false;
        }

        //函数职责：从现有开放接口传播兼容类型，提前排除已与主树断开的剩余模块集合。
        private bool CanStillConnectRemaining(List<ModuleInstance> remaining, List<GiantTombPlacement> placements)
        {
            reachableSignatures.Clear();
            for (int i = 0; i < placements.Count; i++)
            {
                for (int j = 0; j < placements[i].Connectors.Count; j++)
                {
                    GiantTombPlacedConnector connector = placements[i].Connectors[j];
                    if (!connector.Connected) reachableSignatures.Add(Signature(connector.Kind, connector.Cells.Count));
                }
            }
            if (reachableSignatures.Count == 0) return false;

            Array.Clear(reachedBuffer, 0, remaining.Count);
            int reachedCount = 0;
            bool changed;
            do
            {
                changed = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    if (reachedBuffer[i] || !HasCompatibleConnector(remaining[i].Module, reachableSignatures)) continue;
                    reachedBuffer[i] = true;
                    reachedCount++;
                    changed = true;
                    for (int j = 0; j < remaining[i].Module.Connectors.Count; j++)
                    {
                        GiantTombConnector connector = remaining[i].Module.Connectors[j];
                        reachableSignatures.Add(Signature(connector.Kind, connector.Cells.Count));
                    }
                }
            }
            while (changed);
            return reachedCount == remaining.Count;
        }

        //函数职责：判断一个待放模块是否至少有一个接口可接入当前可达接口类型集合。
        private static bool HasCompatibleConnector(GiantTombModule module, HashSet<int> reachableSignatures)
        {
            for (int i = 0; i < module.Connectors.Count; i++)
            {
                GiantTombConnector connector = module.Connectors[i];
                foreach (int signature in reachableSignatures)
                {
                    GiantTombConnectorKind kind = (GiantTombConnectorKind)(signature >> 16);
                    int width = signature & 0xFFFF;
                    if (GiantTombConnectorCompatibility.AreCompatible(kind, width, connector.Kind, connector.Cells.Count)) return true;
                }
            }
            return false;
        }

        //函数职责：把接口类型和宽度压缩为可用于集合传播的稳定整数编码。
        private static int Signature(GiantTombConnectorKind kind, int width)
        {
            return GiantTombConnectorCompatibility.Signature(kind, width);
        }

        //函数职责：检查候选包围盒位于安全边界内且不会与既有非父模块重叠或贴边。
        private bool IsBoundsValid(CellRect bounds, GiantTombPlacement parent)
        {
            if (bounds.minX < borderMargin || bounds.minZ < borderMargin
                || bounds.maxX >= mapWidth - borderMargin || bounds.maxZ >= mapHeight - borderMargin)
            {
                return false;
            }
            return !spatialIndex.Conflicts(bounds, parent);
        }

        //函数职责：为本轮出现的每种模板预计算八种空间变换下每个连接点的尺寸、方向和对齐锚点。
        private void PreparePlacementVariants(IReadOnlyList<GiantTombModule> pool)
        {
            for (int moduleIndex = 0; moduleIndex < pool.Count; moduleIndex++)
            {
                GiantTombModule module = pool[moduleIndex];
                if (variantsByModule.ContainsKey(module)) continue;
                List<PlacementVariant>[] variants =
                {
                    new List<PlacementVariant>(),
                    new List<PlacementVariant>(),
                    new List<PlacementVariant>(),
                    new List<PlacementVariant>()
                };
                foreach (ClMapTransform transform in EnumerateTransforms(module))
                {
                    GiantTombPlacementPrototype prototype = new GiantTombPlacementPrototype(module, transform);
                    for (int connectorIndex = 0; connectorIndex < module.Connectors.Count; connectorIndex++)
                    {
                        GiantTombConnectorPrototype connector = prototype.Connectors[connectorIndex];
                        variants[connector.Direction.AsInt].Add(new PlacementVariant(connectorIndex, prototype, connector.AlignmentCell));
                    }
                }
                variantsByModule.Add(module, variants);
            }
        }

        //函数职责：枚举模板允许的四向旋转与镜像组合。
        private static IEnumerable<ClMapTransform> EnumerateTransforms(GiantTombModule module)
        {
            for (int rotation = 0; rotation < 4; rotation++)
            {
                yield return new ClMapTransform(new Rot4(rotation), false);
                yield return new ClMapTransform(new Rot4(rotation), true);
            }
        }

        //类职责：区分同一模板的必选实例和随机重复实例。
        private sealed class ModuleInstance
        {
            public int Id;
            public GiantTombModule Module;
        }

        //类职责：保存一次已经通过空间校验的递归摆放候选及其纵深评分。
        private sealed class Candidate
        {
            public ModuleInstance Instance;
            public IntVec3 Origin;
            public PlacementVariant Variant;
            public float Score;
        }

        //类职责：保存一次最少候选接口选择及其父模块和可复用候选列表。
        private sealed class FrontierChoice
        {
            public GiantTombPlacement Parent;
            public GiantTombPlacedConnector Frontier;
            public List<Candidate> Candidates;
        }

        //类职责：保存一个模板连接点在指定旋转镜像下可复用的空间计算结果。
        private sealed class PlacementVariant
        {
            public readonly int ConnectorIndex;
            public readonly GiantTombPlacementPrototype Prototype;
            public readonly IntVec3 AlignmentCell;
            public IntVec2 Size => Prototype.Size;

            //函数职责：建立可直接用于候选边界预检和连接点对齐的变换记录。
            public PlacementVariant(int connectorIndex, GiantTombPlacementPrototype prototype, IntVec3 alignmentCell)
            {
                ConnectorIndex = connectorIndex;
                Prototype = prototype;
                AlignmentCell = alignmentCell;
            }
        }
    }
}
