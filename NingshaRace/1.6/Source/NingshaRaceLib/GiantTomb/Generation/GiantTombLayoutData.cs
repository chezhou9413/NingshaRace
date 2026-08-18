using System.Collections;
using System.Collections.Generic;
using NingshaRaceLib.GiantTomb.Layout;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation
{
    //类职责：保存本次地图生成求得的模块实例、连接关系、结构占用格和入口信息。
    internal sealed class GiantTombLayoutData
    {
        public readonly List<GiantTombPlacement> Placements = new List<GiantTombPlacement>();
        public readonly List<GiantTombConnection> Connections = new List<GiantTombConnection>();
        public BitArray StructureCells;
        public GiantTombPlacement Entrance;

        //函数职责：判断指定地图格是否属于任意墓葬模块结构。
        public bool Contains(IntVec3 cell, Map map)
        {
            return cell.InBounds(map) && StructureCells[map.cellIndices.CellToIndex(cell)];
        }
    }
}
