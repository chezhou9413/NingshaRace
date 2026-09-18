using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Topology;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：记录生态区的实际轮廓、水面和内容标记，供地形与植物步骤共用。
    public sealed class DesertPitHabitat
    {
        public DesertPitRoom room;
        public bool marsh;
        public bool sandfall;
        public bool pond;
        public readonly HashSet<IntVec3> floor = new HashSet<IntVec3>();
        public readonly HashSet<IntVec3> water = new HashSet<IntVec3>();
    }
}
