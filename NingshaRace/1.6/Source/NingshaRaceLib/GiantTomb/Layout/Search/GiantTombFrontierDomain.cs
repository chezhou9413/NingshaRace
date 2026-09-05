using System;
using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：缓存一个出口的候选集合，只检查此后加入的房间，并支持回溯恢复。
    internal sealed class GiantTombFrontierDomain
    {
        public readonly GiantTombPlacement Parent;
        public readonly GiantTombPlacedConnector Connector;
        private readonly int creationCount;
        private GiantTombSpatialCandidate[] candidates;
        private bool[] blocked;
        private int checkedCount;

        //职责：记录出口所属房间与不可撤销的祖先前缀，延迟准备尚未参与选择的出口。
        public GiantTombFrontierDomain(GiantTombPlacement parent, GiantTombPlacedConnector connector, int placementCount)
        {
            Parent = parent;
            Connector = connector;
            creationCount = checkedCount = placementCount;
        }

        //职责：只为此出口计算一次匹配接口、坐标和祖先碰撞，保留今后可能重新可用的模板类别。
        public void Prepare(GiantTombSearchCatalog catalog, List<GiantTombPlacement> placements,
            GiantTombPlacementSpatialIndex spatialIndex, int width, int height, int margin, ref long checks)
        {
            if (candidates != null) return;
            List<GiantTombSpatialCandidate> result = new List<GiantTombSpatialCandidate>();
            GiantTombPlacementVariant[] variants = catalog.Facing(Connector.Direction.Opposite);
            IntVec3 anchor = Connector.AlignmentCell + Connector.Direction.FacingCell;
            for (int i = 0; i < variants.Length; i++)
            {
                GiantTombPlacementVariant variant = variants[i];
                if (!GiantTombConnectorCompatibility.AreCompatible(Connector.Kind, Connector.Cells.Count, variant.Kind, variant.Width)) continue;
                GiantTombSpatialCandidate candidate = new GiantTombSpatialCandidate(variant, anchor - variant.AlignmentCell);
                CellRect bounds = candidate.Bounds;
                if (bounds.minX < margin || bounds.minZ < margin || bounds.maxX >= width - margin || bounds.maxZ >= height - margin) continue;
                bool conflict = false;
                if (placements.Count == creationCount)
                {
                    conflict = spatialIndex.Conflicts(bounds, Parent);
                }
                else
                {
                    //延迟准备的出口只能永久排除创建时已有的房间，否则回溯会错误丢失候选。
                    for (int j = 0; j < creationCount; j++)
                    {
                        checks++;
                        GiantTombPlacement existing = placements[j];
                        if ((existing == Parent ? bounds : candidate.BufferedBounds).Overlaps(existing.Bounds))
                        {
                            conflict = true;
                            break;
                        }
                    }
                }
                if (!conflict) result.Add(candidate);
            }
            candidates = result.ToArray();
            blocked = new bool[candidates.Length];
        }

        //职责：只让尚未检查的新房间筛除候选，并把失效原因登记到对应房间的撤销日志。
        public void Refresh(List<GiantTombPlacement> placements, List<GiantTombDomainBlock>[] undo, ref long checks)
        {
            for (int roomIndex = checkedCount; roomIndex < placements.Count; roomIndex++)
            {
                CellRect bounds = placements[roomIndex].Bounds;
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (blocked[index]) continue;
                    checks++;
                    if (!candidates[index].BufferedBounds.Overlaps(bounds)) continue;
                    blocked[index] = true;
                    undo[roomIndex].Add(new GiantTombDomainBlock(blocked, index));
                }
            }
            checkedCount = placements.Count;
        }

        //职责：统计几何可用且仍有未摆放实例的候选，在超过当前最优计数时提前结束。
        public int Count(Stack<int>[] instances, int stopAfter)
        {
            int count = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!blocked[i] && instances[candidates[i].Variant.ModuleIndex].Count > 0 && ++count > stopAfter) break;
            }
            return count;
        }

        //职责：复制选定出口的有效候选用于排序，不再次执行几何或碰撞计算。
        public List<GiantTombSpatialCandidate> Collect(Stack<int>[] instances)
        {
            List<GiantTombSpatialCandidate> result = new List<GiantTombSpatialCandidate>();
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!blocked[i] && instances[candidates[i].Variant.ModuleIndex].Count > 0) result.Add(candidates[i]);
            }
            return result;
        }

        //职责：撤销房间后缩短检查前缀，使同一深度的其他候选房间仍会接受检查。
        public void Rewind(int placementCount)
        {
            checkedCount = Math.Min(checkedCount, placementCount);
        }
    }
}
