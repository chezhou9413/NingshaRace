using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Topology
{
    //类职责：记录一个自然洞室的形状参数和实际地面，供连通检查及菌巢选址复用。
    public sealed class DesertPitRoom
    {
        public IntVec3 Center;
        public float RadiusX;
        public float RadiusZ;
        public float Rotation;
        public readonly HashSet<IntVec3> Floor = new HashSet<IntVec3>();
    }
}
