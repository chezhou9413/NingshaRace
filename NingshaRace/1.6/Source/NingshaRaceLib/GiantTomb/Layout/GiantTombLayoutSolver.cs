using System;
using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：用可撤销出口候选和受限随机回溯，将全部模板实例拼成接口闭合的纵深树。
    internal sealed class GiantTombLayoutSolver
    {
        private readonly int mapWidth;
        private readonly int mapHeight;
        private readonly int borderMargin;
        private readonly GiantTombSearchCatalog catalog;
        private readonly GiantTombModule[] terminalModules;
        private readonly GiantTombLayoutRandom random;
        private readonly Func<bool> shouldStop;
        private GiantTombPlacementSpatialIndex spatialIndex;
        private GiantTombFrontierSet frontiers;
        private GiantTombConnectivityCheck connectivity;
        private Stack<int>[] instances;
        private int evaluations;
        private int evaluationLimit;
        private int deepestPlacementCount;

        public int Evaluations => evaluations;
        public int DeepestPlacementCount => deepestPlacementCount;
        public long CollisionChecks => spatialIndex.PairChecks + frontiers.CollisionChecks;

        //职责：接收冻结的共享几何与局部种子，每个求解器独占自己的可变搜索状态。
        public GiantTombLayoutSolver(int mapWidth, int mapHeight, int borderMargin, GiantTombSearchCatalog catalog,
            GiantTombModule[] terminalModules, int randomSeed, Func<bool> shouldStop = null)
        {
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
            this.borderMargin = borderMargin;
            this.catalog = catalog;
            this.terminalModules = terminalModules;
            this.shouldStop = shouldStop;
            random = new GiantTombLayoutRandom(randomSeed);
        }

        //职责：在本次预算内求解完整布局，失败时不向调用者公开半成品房间集合。
        public bool TrySolve(IReadOnlyList<GiantTombModule> pool, GiantTombModule entrance, int candidateBudget,
            out List<GiantTombPlacement> placements, out List<GiantTombConnection> connections)
        {
            evaluations = 0;
            evaluationLimit = candidateBudget;
            deepestPlacementCount = 1;
            instances = BuildInstances(pool, entrance);
            spatialIndex = new GiantTombPlacementSpatialIndex(mapWidth, mapHeight, pool.Count + 1);
            frontiers = new GiantTombFrontierSet(catalog, mapWidth, mapHeight, borderMargin, pool.Count);
            connectivity = new GiantTombConnectivityCheck(catalog.Modules);
            placements = new List<GiantTombPlacement>(pool.Count);
            connections = new List<GiantTombConnection>(pool.Count - 1);
            GiantTombPlacement root = BuildRoot(entrance);
            placements.Add(root);
            spatialIndex.Add(root);
            frontiers.Add(root, placements.Count);
            if (Search(pool.Count - 1, placements, connections)) return true;
            placements = null;
            connections = null;
            return false;
        }

        //职责：按模板类别存放稳定实例编号，使取用和回溯恢复均为常数时间。
        private Stack<int>[] BuildInstances(IReadOnlyList<GiantTombModule> pool, GiantTombModule entrance)
        {
            Stack<int>[] result = new Stack<int>[catalog.Modules.Length];
            for (int i = 0; i < result.Length; i++) result[i] = new Stack<int>();
            int rootIndex = -1;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != entrance) continue;
                rootIndex = i;
                break;
            }
            if (rootIndex < 0) throw new InvalidOperationException("墓葬模板池缺少入口实例");
            for (int i = pool.Count - 1; i >= 0; i--)
                if (i != rootIndex) result[catalog.IndexOf(pool[i])].Push(i + 1);
            return result;
        }

        //职责：在许可变换中选择朝北入口，验证入口处于地图南部安全边界内。
        private GiantTombPlacement BuildRoot(GiantTombModule entrance)
        {
            if (entrance.Connectors.Count != 1) throw new InvalidOperationException("巨型墓葬入口模板必须恰好有一个连接点");
            List<GiantTombPlacementVariant> choices = new List<GiantTombPlacementVariant>();
            foreach (GiantTombPlacementVariant variant in catalog.Facing(Rot4.North))
                if (catalog.Modules[variant.ModuleIndex] == entrance) choices.Add(variant);
            if (choices.Count == 0) throw new InvalidOperationException("巨型墓葬入口模板许可的变换无法朝北");
            random.Shuffle(choices);
            GiantTombPlacementPrototype prototype = choices[0].Prototype;
            int x = (mapWidth - prototype.Size.x) / 2 + random.RangeInclusive(-4, 4);
            GiantTombPlacement root = prototype.Build(entrance, new IntVec3(x, 0, borderMargin), 0, 0);
            if (root.Bounds.minX < borderMargin || root.Bounds.maxX >= mapWidth - borderMargin
                || root.Bounds.maxZ >= mapHeight - borderMargin)
                throw new InvalidOperationException("墓葬入口模板无法容纳在地图安全边界内");
            return root;
        }

        //职责：优先处理候选最少的出口，递归摆放并精确撤销失败分支的实例和候选状态。
        private bool Search(int remaining, List<GiantTombPlacement> placements, List<GiantTombConnection> connections)
        {
            if (shouldStop != null && shouldStop()) return false;
            deepestPlacementCount = Math.Max(deepestPlacementCount, placements.Count);
            if (remaining == 0)
            {
                foreach (GiantTombFrontierDomain domain in frontiers.Domains)
                    if (!domain.Connector.Connected) return false;
                return true;
            }
            if (evaluations >= evaluationLimit || !connectivity.CanConnect(instances, frontiers.Domains)) return false;
            GiantTombFrontierDomain choice = frontiers.Select(placements, instances, spatialIndex, random, shouldStop);
            if (choice == null) return false;
            List<GiantTombSpatialCandidate> spatial = choice.Collect(instances);
            List<GiantTombScoredCandidate> candidates = new List<GiantTombScoredCandidate>(spatial.Count);
            for (int i = 0; i < spatial.Count; i++)
                candidates.Add(new GiantTombScoredCandidate(spatial[i], Score(spatial[i], choice.Parent.Depth + 1, remaining), i));
            candidates.Sort(GiantTombScoredCandidate.Compare);
            int count = Math.Min(256, candidates.Count);
            for (int i = 0; i < count && evaluations < evaluationLimit; i++)
            {
                if (shouldStop != null && shouldStop()) return false;
                evaluations++;
                GiantTombSpatialCandidate candidate = candidates[i].Spatial;
                int moduleIndex = candidate.Variant.ModuleIndex;
                int instanceId = instances[moduleIndex].Pop();
                GiantTombPlacement child = candidate.Variant.Prototype.Build(catalog.Modules[moduleIndex],
                    candidate.Origin, instanceId, choice.Parent.Depth + 1);
                GiantTombPlacedConnector connector = child.Connectors[candidate.Variant.ConnectorIndex];
                choice.Connector.Connected = connector.Connected = true;
                int previousDomainCount = frontiers.Count;
                placements.Add(child);
                spatialIndex.Add(child);
                frontiers.Add(child, placements.Count);
                connections.Add(new GiantTombConnection
                {
                    Parent = choice.Parent, ParentConnector = choice.Connector, Child = child, ChildConnector = connector
                });
                if (Search(remaining - 1, placements, connections)) return true;
                //先恢复子房间造成的失效，再释放房间和实例，防止兄弟分支读取旧的碰撞结果。
                frontiers.Rollback(placements.Count - 1, previousDomainCount);
                spatialIndex.Remove(child);
                placements.RemoveAt(placements.Count - 1);
                connections.RemoveAt(connections.Count - 1);
                instances[moduleIndex].Push(instanceId);
                choice.Connector.Connected = connector.Connected = false;
            }
            return false;
        }

        //职责：保留面积、分支能力、北向纵深和终点偏好，仅对通过前向检查的候选评分。
        private float Score(GiantTombSpatialCandidate candidate, int depth, int remainingCount)
        {
            CellRect bounds = candidate.Bounds;
            GiantTombModule module = catalog.Modules[candidate.Variant.ModuleIndex];
            int clearance = Math.Min(Math.Min(bounds.minX - borderMargin, bounds.minZ - borderMargin),
                Math.Min(mapWidth - borderMargin - 1 - bounds.maxX, mapHeight - borderMargin - 1 - bounds.maxZ));
            int area = bounds.Width * bounds.Height;
            float score = bounds.maxZ * 0.75f + module.Connectors.Count * 90f + clearance * 1.5f
                + area * (remainingCount > 12 ? 0.45f : 0.1f) + random.Value();
            for (int i = 0; i < terminalModules.Length; i++)
            {
                if (terminalModules[i] != module) continue;
                score += depth * 60f;
                if (remainingCount > 16) score -= 300f;
                break;
            }
            return score;
        }
    }
}
