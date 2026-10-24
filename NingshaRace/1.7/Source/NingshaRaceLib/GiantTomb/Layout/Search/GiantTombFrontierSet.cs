using System;
using System.Collections.Generic;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：维护开放出口、最少候选选择与按房间分组的碰撞撤销日志。
    internal sealed class GiantTombFrontierSet
    {
        private readonly GiantTombSearchCatalog catalog;
        private readonly int width;
        private readonly int height;
        private readonly int margin;
        private readonly List<GiantTombDomainBlock>[] undo;
        private readonly List<GiantTombFrontierDomain> domains = new List<GiantTombFrontierDomain>();
        private long collisionChecks;
        public long CollisionChecks => collisionChecks;
        public int Count => domains.Count;
        public IReadOnlyList<GiantTombFrontierDomain> Domains => domains;

        //职责：建立单个求解器独占的可撤销缓存，不把可变数据共享给其他线程。
        public GiantTombFrontierSet(GiantTombSearchCatalog catalog, int width, int height, int margin, int roomCount)
        {
            this.catalog = catalog;
            this.width = width;
            this.height = height;
            this.margin = margin;
            undo = new List<GiantTombDomainBlock>[roomCount];
            for (int i = 0; i < roomCount; i++) undo[i] = new List<GiantTombDomainBlock>();
        }

        //职责：登记刚摆放房间的未连接出口，保持稳定的房间和接口遍历顺序。
        public void Add(GiantTombPlacement placement, int placementCount)
        {
            for (int i = 0; i < placement.Connectors.Count; i++)
                if (!placement.Connectors[i].Connected)
                    domains.Add(new GiantTombFrontierDomain(placement, placement.Connectors[i], placementCount));
        }

        //职责：前向检查开放出口，优先选择真实候选最少的一项并剪去无候选分支。
        public GiantTombFrontierDomain Select(List<GiantTombPlacement> placements, Stack<int>[] instances,
            GiantTombPlacementSpatialIndex spatialIndex, GiantTombLayoutRandom random, Func<bool> shouldStop)
        {
            GiantTombFrontierDomain selected = null;
            int minimum = int.MaxValue;
            for (int i = 0; i < domains.Count; i++)
            {
                GiantTombFrontierDomain domain = domains[i];
                if (domain.Connector.Connected) continue;
                if (shouldStop != null && shouldStop()) return null;
                domain.Prepare(catalog, placements, spatialIndex, width, height, margin, ref collisionChecks);
                domain.Refresh(placements, undo, ref collisionChecks);
                int count = domain.Count(instances, minimum);
                if (count == 0) return domain;
                if (selected == null || count < minimum || count == minimum && random.Bool())
                {
                    minimum = count;
                    selected = domain;
                }
            }
            return selected;
        }

        //职责：回退最后一个房间，恢复它阻挡的候选并丢弃只属于该房间的出口。
        public void Rollback(int placementIndex, int previousDomainCount)
        {
            List<GiantTombDomainBlock> changes = undo[placementIndex];
            for (int i = 0; i < changes.Count; i++) changes[i].Restore();
            changes.Clear();
            domains.RemoveRange(previousDomainCount, domains.Count - previousDomainCount);
            for (int i = 0; i < domains.Count; i++) domains[i].Rewind(placementIndex);
        }
    }
}
