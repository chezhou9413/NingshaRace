using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：缓存一个模板空间变换后的尺寸与连接点局部坐标，供回溯分支低分配构造房间实例。
    internal sealed class GiantTombPlacementPrototype
    {
        public readonly ClMapTransform Transform;
        public readonly IntVec2 Size;
        public readonly GiantTombConnectorPrototype[] Connectors;

        //函数职责：一次性计算指定模板变换后的全部连接点几何数据。
        public GiantTombPlacementPrototype(GiantTombModule module, ClMapTransform transform)
        {
            Transform = transform;
            Size = transform.GetOutputSize(module.Width, module.Height);
            Connectors = new GiantTombConnectorPrototype[module.Connectors.Count];
            for (int i = 0; i < module.Connectors.Count; i++)
            {
                Connectors[i] = new GiantTombConnectorPrototype(module, module.Connectors[i], transform);
            }
        }

        //函数职责：把已缓存的局部连接点平移到地图坐标并建立可参与递归的房间实例。
        public GiantTombPlacement Build(GiantTombModule module, IntVec3 origin, int instanceId, int depth)
        {
            GiantTombPlacement placement = new GiantTombPlacement
            {
                InstanceId = instanceId,
                Module = module,
                Origin = origin,
                Transform = Transform,
                Bounds = new CellRect(origin.x, origin.z, Size.x, Size.z),
                Depth = depth
            };
            for (int i = 0; i < Connectors.Length; i++)
            {
                GiantTombConnectorPrototype source = Connectors[i];
                GiantTombPlacedConnector connector = new GiantTombPlacedConnector
                {
                    Index = i,
                    Kind = source.Kind,
                    Direction = source.Direction,
                    AlignmentCell = origin + source.AlignmentCell
                };
                for (int j = 0; j < source.Cells.Length; j++)
                {
                    connector.Cells.Add(origin + source.Cells[j]);
                }
                placement.Connectors.Add(connector);
            }
            return placement;
        }
    }

    //类职责：保存单个连接点完成旋转镜像后的有序局部格、朝向和中心对齐格。
    internal sealed class GiantTombConnectorPrototype
    {
        public readonly GiantTombConnectorKind Kind;
        public readonly Rot4 Direction;
        public readonly IntVec3[] Cells;
        public readonly IntVec3 AlignmentCell;

        //函数职责：把模板连接点转换为指定空间变换下可直接平移的局部数据。
        public GiantTombConnectorPrototype(GiantTombModule module, GiantTombConnector source, ClMapTransform transform)
        {
            Kind = source.Kind;
            Direction = transform.TransformRotation(source.Direction);
            List<IntVec3> cells = TransformAndSort(module, source.Cells, transform);
            Cells = cells.ToArray();
            List<IntVec3> alignmentCells = TransformAndSort(module, source.AlignmentCells, transform);
            AlignmentCell = alignmentCells[alignmentCells.Count / 2];
        }

        //函数职责：变换并稳定排序连接点跨度格，保证镜像后仍使用同一中心规则。
        private static List<IntVec3> TransformAndSort(GiantTombModule module, List<IntVec3> source, ClMapTransform transform)
        {
            List<IntVec3> result = new List<IntVec3>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                IntVec3 cell = source[i];
                result.Add(transform.TransformCell(cell.z * module.Width + cell.x, module.Width, module.Height));
            }
            result.Sort((left, right) => left.x != right.x ? left.x.CompareTo(right.x) : left.z.CompareTo(right.z));
            return result;
        }
    }
}
