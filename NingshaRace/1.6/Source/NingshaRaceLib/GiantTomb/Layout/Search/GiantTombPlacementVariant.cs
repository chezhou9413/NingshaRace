using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：保存一个可共享的模板变换接口，不携带任何搜索分支状态。
    internal sealed class GiantTombPlacementVariant
    {
        public readonly int ModuleIndex;
        public readonly int ConnectorIndex;
        public readonly GiantTombPlacementPrototype Prototype;
        public readonly GiantTombConnectorKind Kind;
        public readonly int Width;
        public readonly IntVec3 AlignmentCell;

        //职责：从已冻结的几何原型提取接口匹配和坐标计算所需的数据。
        public GiantTombPlacementVariant(int moduleIndex, int connectorIndex, GiantTombPlacementPrototype prototype)
        {
            ModuleIndex = moduleIndex;
            ConnectorIndex = connectorIndex;
            Prototype = prototype;
            GiantTombConnectorPrototype connector = prototype.Connectors[connectorIndex];
            Kind = connector.Kind;
            Width = connector.Cells.Length;
            AlignmentCell = connector.AlignmentCell;
        }
    }
}
