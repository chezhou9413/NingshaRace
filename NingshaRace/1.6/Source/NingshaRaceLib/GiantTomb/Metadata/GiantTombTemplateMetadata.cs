using System.Collections.Generic;

namespace NingshaRaceLib.GiantTomb.Metadata
{
    //类职责：映射ChezhouLib地图模板metadata v2中巨型墓葬运行时需要的字段。
    internal sealed class GiantTombTemplateMetadata
    {
        public string Schema { get; set; }

        public int Version { get; set; }

        public string Name { get; set; }

        public string SourceFile { get; set; }

        public long BinaryBytes { get; set; }

        public GiantTombTemplateSize TemplateSize { get; set; }

        public List<string> WallDoorGrid { get; set; }

        public List<string> OccupancyGrid { get; set; }

        public List<string> WalkabilityGrid { get; set; }

        public List<GiantTombConnectorMetadata> InferredConnectors { get; set; }
    }

    //类职责：保存metadata声明的模板格网尺寸。
    internal sealed class GiantTombTemplateSize
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int CellCount { get; set; }
    }

    //类职责：保存metadata推断出的一个边界连接点及其连续跨度。
    internal sealed class GiantTombConnectorMetadata
    {
        public string Type { get; set; }

        public string Direction { get; set; }

        public int X { get; set; }

        public int Z { get; set; }

        public int Width { get; set; }

        public int StartX { get; set; }

        public int StartZ { get; set; }

        public int EndX { get; set; }

        public int EndZ { get; set; }
    }
}
