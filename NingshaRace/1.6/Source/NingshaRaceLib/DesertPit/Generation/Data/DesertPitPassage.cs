using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Data
{
    //类职责：保存一条连接通道的有序中心线和真实笔刷占地，供独立面积配额使用。
    internal sealed class DesertPitPassage
    {
        public readonly List<IntVec3> Centerline;
        public readonly HashSet<IntVec3> Floor;

        //函数职责：复制通道形状，避免后续笔刷复用临时集合时改变资源区域。
        public DesertPitPassage(IEnumerable<IntVec3> centerline, IEnumerable<IntVec3> floor)
        {
            Centerline = new List<IntVec3>(centerline);
            Floor = new HashSet<IntVec3>(floor);
        }
    }
}
