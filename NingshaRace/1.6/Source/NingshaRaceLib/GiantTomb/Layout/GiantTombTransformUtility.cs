using System;
using System.Collections.Generic;
using System.Linq;
using ChezhouLib.CustomMission.MapTemplates;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：统一完成模块、结构掩码和连接点从模板局部坐标到地图坐标的转换。
    internal static class GiantTombTransformUtility
    {
        //函数职责：根据模块、原点和变换建立一个完整的地图实例记录。
        public static GiantTombPlacement BuildPlacement(GiantTombModule module, IntVec3 origin, ClMapTransform transform, int instanceId, int depth)
        {
            IntVec2 size = transform.GetOutputSize(module.Width, module.Height);
            GiantTombPlacement placement = new GiantTombPlacement
            {
                InstanceId = instanceId,
                Module = module,
                Origin = origin,
                Transform = transform,
                Bounds = new CellRect(origin.x, origin.z, size.x, size.z),
                Depth = depth
            };
            for (int i = 0; i < module.Connectors.Count; i++)
            {
                GiantTombConnector source = module.Connectors[i];
                GiantTombPlacedConnector connector = new GiantTombPlacedConnector
                {
                    Index = i,
                    Kind = source.Kind,
                    Direction = transform.TransformRotation(source.Direction)
                };
                for (int j = 0; j < source.Cells.Count; j++)
                {
                    IntVec3 local = source.Cells[j];
                    int index = local.z * module.Width + local.x;
                    connector.Cells.Add(origin + transform.TransformCell(index, module.Width, module.Height));
                }
                connector.Cells = SortSpan(connector.Cells);
                List<IntVec3> alignmentCells = new List<IntVec3>();
                for (int j = 0; j < source.AlignmentCells.Count; j++)
                {
                    IntVec3 local = source.AlignmentCells[j];
                    int index = local.z * module.Width + local.x;
                    alignmentCells.Add(origin + transform.TransformCell(index, module.Width, module.Height));
                }
                alignmentCells = SortSpan(alignmentCells);
                connector.AlignmentCell = alignmentCells[alignmentCells.Count / 2];
                placement.Connectors.Add(connector);
            }
            return placement;
        }

        //函数职责：计算子模块连接点与父连接点相邻并居中对齐时所需的地图原点。
        public static IntVec3 AlignOrigin(GiantTombPlacedConnector parent, GiantTombModule child, ClMapTransform transform, int childConnectorIndex)
        {
            GiantTombConnector source = child.Connectors[childConnectorIndex];
            List<IntVec3> childAlignmentCells = new List<IntVec3>();
            for (int i = 0; i < source.AlignmentCells.Count; i++)
            {
                IntVec3 local = source.AlignmentCells[i];
                childAlignmentCells.Add(transform.TransformCell(local.z * child.Width + local.x, child.Width, child.Height));
            }
            childAlignmentCells = SortSpan(childAlignmentCells);
            IntVec3 childAnchor = childAlignmentCells[childAlignmentCells.Count / 2];
            IntVec3 targetAnchor = parent.AlignmentCell + parent.Direction.FacingCell;
            return targetAnchor - childAnchor;
        }

        //函数职责：枚举结构掩码中所有启用格经过变换后的地图坐标。
        public static IEnumerable<IntVec3> StructureCells(GiantTombPlacement placement)
        {
            GiantTombModule module = placement.Module;
            for (int index = 0; index < module.StructureMask.Length; index++)
            {
                if (module.StructureMask[index])
                {
                    yield return placement.Origin + placement.Transform.TransformCell(index, module.Width, module.Height);
                }
            }
        }

        //函数职责：按跨度轴稳定排序连接点格子，使不同朝向能够使用中心格对齐。
        private static List<IntVec3> SortSpan(IEnumerable<IntVec3> cells)
        {
            return cells.OrderBy((IntVec3 cell) => cell.x).ThenBy((IntVec3 cell) => cell.z).ToList();
        }
    }
}
