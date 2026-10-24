using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：组合一个已编译地图模板、严格metadata、结构掩码和作者分类标签。
    internal sealed class GiantTombModule
    {
        public ClMapTemplateDef Def;
        public ClCompiledMapTemplate Template;
        public string MetadataPath;
        public bool[] StructureMask;
        public List<GiantTombConnector> Connectors = new List<GiantTombConnector>();

        public int Width => Template.Width;
        public int Height => Template.Height;
    }

    //类职责：保存一个模板实例的地图位置、空间变换、包围盒和已变换连接点。
    internal sealed class GiantTombPlacement
    {
        public int InstanceId;
        public GiantTombModule Module;
        public IntVec3 Origin;
        public ClMapTransform Transform;
        public CellRect Bounds;
        public int Depth;
        public List<GiantTombPlacedConnector> Connectors = new List<GiantTombPlacedConnector>();
    }

    //类职责：记录两块模块之间已经建立的连接及子侧重复门清理信息。
    internal sealed class GiantTombConnection
    {
        public GiantTombPlacement Parent;
        public GiantTombPlacedConnector ParentConnector;
        public GiantTombPlacement Child;
        public GiantTombPlacedConnector ChildConnector;
    }
}
