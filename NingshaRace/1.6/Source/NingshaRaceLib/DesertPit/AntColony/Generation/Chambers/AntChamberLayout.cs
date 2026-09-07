using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：把自然洞室形状映射到地图朝向，分开记录选址包围盒、实际占地与唯一洞口。
    public sealed class AntChamberLayout
    {
        public IntVec3 Origin;
        public int Direction;
        public CellRect Bounds;
        public readonly HashSet<IntVec3> OpenCells = new HashSet<IntVec3>();
        public readonly HashSet<IntVec3> PassageCells = new HashSet<IntVec3>();
        public readonly HashSet<IntVec3> Footprint = new HashSet<IntVec3>();
        public readonly HashSet<IntVec3> GatewayCells = new HashSet<IntVec3>();
        public IntVec3 Nest;
        public IntVec3 Mound;
        public IntVec3 Mouth;

        //函数职责：以四种朝向将洞室局部坐标转换为地图坐标。
        public IntVec3 At(int x, int z)
        {
            return Origin + Rotate(new IntVec3(x, 0, z), Direction);
        }

        //函数职责：对局部格作四向旋转，保持地面、岩层和场景位置使用同一坐标变换。
        internal static IntVec3 Rotate(IntVec3 cell, int direction)
        {
            switch (direction)
            {
                case 0: return cell;
                case 1: return new IntVec3(-cell.z, 0, cell.x);
                case 2: return new IntVec3(-cell.x, 0, -cell.z);
                default: return new IntVec3(cell.z, 0, -cell.x);
            }
        }

        //函数职责：仅在选址完成后实例化世界格集合，避免为每个候选分配大量集合。
        internal AntChamberLayout(IntVec3 origin, int direction, AntChamberShape shape)
        {
            Origin = origin;
            Direction = direction;
            Bounds = BoundsAt(shape.Bounds, origin, direction);
            Nest = At(shape.Nest.x, shape.Nest.z);
            Mound = At(shape.Mound.x, shape.Mound.z);
            Mouth = At(shape.Mouth.x, shape.Mouth.z);
            foreach (IntVec3 cell in shape.OpenCells) OpenCells.Add(At(cell.x, cell.z));
            foreach (IntVec3 cell in shape.PassageCells) PassageCells.Add(At(cell.x, cell.z));
            foreach (IntVec3 cell in shape.Footprint) Footprint.Add(At(cell.x, cell.z));
            foreach (IntVec3 cell in shape.OpenCells)
                if (cell.x == shape.Mouth.x) GatewayCells.Add(At(cell.x, cell.z));
        }

        //函数职责：计算候选朝向的矩形范围，只用于快速选址而不作为岩墙形状。
        internal static CellRect BoundsAt(CellRect bounds, IntVec3 origin, int direction)
        {
            return CellRect.FromLimits(origin + Rotate(new IntVec3(bounds.minX, 0, bounds.minZ), direction),
                origin + Rotate(new IntVec3(bounds.maxX, 0, bounds.maxZ), direction));
        }
    }
}
