using System;
using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：用固定分桶索引加速布局求解期间的房间矩形碰撞查询，避免每个候选扫描全部既有房间。
    internal sealed class GiantTombPlacementSpatialIndex
    {
        private const int BucketSize = 16;
        private readonly int mapWidth;
        private readonly int mapHeight;
        private readonly int bucketColumns;
        private readonly List<GiantTombPlacement>[] buckets;
        private readonly int[] visitedStamps;
        private int queryStamp;
        public long PairChecks { get; private set; }

        //函数职责：按地图尺寸和本轮最大实例编号建立只供单个后台求解器使用的空间索引。
        public GiantTombPlacementSpatialIndex(int mapWidth, int mapHeight, int maximumInstanceId)
        {
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
            bucketColumns = (mapWidth + BucketSize - 1) / BucketSize;
            int bucketRows = (mapHeight + BucketSize - 1) / BucketSize;
            buckets = new List<GiantTombPlacement>[bucketColumns * bucketRows];
            visitedStamps = new int[maximumInstanceId + 1];
        }

        //函数职责：把已确定房间登记到其包围盒覆盖的全部分桶。
        public void Add(GiantTombPlacement placement)
        {
            GetBucketRange(placement.Bounds, out int minX, out int minZ, out int maxX, out int maxZ);
            for (int z = minZ; z <= maxZ; z++)
            {
                int row = z * bucketColumns;
                for (int x = minX; x <= maxX; x++)
                {
                    int bucket = row + x;
                    List<GiantTombPlacement> entries = buckets[bucket];
                    if (entries == null)
                    {
                        entries = new List<GiantTombPlacement>(4);
                        buckets[bucket] = entries;
                    }
                    entries.Add(placement);
                }
            }
        }

        //函数职责：在回溯时从全部相关分桶移除指定房间实例。
        public void Remove(GiantTombPlacement placement)
        {
            GetBucketRange(placement.Bounds, out int minX, out int minZ, out int maxX, out int maxZ);
            for (int z = minZ; z <= maxZ; z++)
            {
                int row = z * bucketColumns;
                for (int x = minX; x <= maxX; x++)
                {
                    buckets[row + x].Remove(placement);
                }
            }
        }

        //函数职责：检查候选是否与任何房间重叠，或与父房间之外的房间贴边。
        public bool Conflicts(CellRect bounds, GiantTombPlacement parent)
        {
            int stamp = NextQueryStamp();
            CellRect queryBounds = bounds.ExpandedBy(1);
            GetBucketRange(queryBounds, out int minX, out int minZ, out int maxX, out int maxZ);
            for (int z = minZ; z <= maxZ; z++)
            {
                int row = z * bucketColumns;
                for (int x = minX; x <= maxX; x++)
                {
                    List<GiantTombPlacement> entries = buckets[row + x];
                    if (entries == null)
                    {
                        continue;
                    }
                    for (int i = 0; i < entries.Count; i++)
                    {
                        GiantTombPlacement existing = entries[i];
                        int instanceId = existing.InstanceId;
                        if (visitedStamps[instanceId] == stamp)
                        {
                            continue;
                        }
                        visitedStamps[instanceId] = stamp;
                        PairChecks++;
                        if (bounds.Overlaps(existing.Bounds)
                            || existing != parent && queryBounds.Overlaps(existing.Bounds))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //函数职责：把矩形换算为裁剪在地图范围内的分桶坐标区间。
        private void GetBucketRange(CellRect bounds, out int minX, out int minZ, out int maxX, out int maxZ)
        {
            minX = Math.Max(0, bounds.minX) / BucketSize;
            minZ = Math.Max(0, bounds.minZ) / BucketSize;
            maxX = Math.Min(mapWidth - 1, bounds.maxX) / BucketSize;
            maxZ = Math.Min(mapHeight - 1, bounds.maxZ) / BucketSize;
        }

        //函数职责：生成本次查询的去重标记，并在整数回绕前清空旧标记。
        private int NextQueryStamp()
        {
            if (queryStamp == int.MaxValue)
            {
                Array.Clear(visitedStamps, 0, visitedStamps.Length);
                queryStamp = 0;
            }
            return ++queryStamp;
        }
    }
}
