using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //枚举职责：区分需要保留门体的门连接点和直接贯通的开放连接点。
    internal enum GiantTombConnectorKind
    {
        Door,
        Open
    }

    //类职责：保存模板局部坐标中的已验证连接点。
    internal sealed class GiantTombConnector
    {
        public GiantTombConnectorKind Kind;
        public Rot4 Direction;
        public List<IntVec3> Cells = new List<IntVec3>();
        public List<IntVec3> AlignmentCells = new List<IntVec3>();
    }

    //类职责：保存模块实例变换到地图坐标后的连接点。
    internal sealed class GiantTombPlacedConnector
    {
        public int Index;
        public GiantTombConnectorKind Kind;
        public Rot4 Direction;
        public List<IntVec3> Cells = new List<IntVec3>();
        public IntVec3 AlignmentCell;
        public bool Connected;
    }
}
