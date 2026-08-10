using System.Collections.Generic;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Steps;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Data
{
    //类职责：保存沙漠巨坑生成期间跨 GenStep 共享的主洞室、小洞室和特殊地貌位置。
    public class DesertPitLayoutData
    {
        //字段职责：记录主洞室中心，供入口、散布和安全清理逻辑使用。
        public IntVec3 MainCenter;

        //字段职责：记录主洞室横向半径，供主洞轮廓雕刻使用。
        public float MainRadiusX;

        //字段职责：记录主洞室纵向半径，供主洞轮廓雕刻使用。
        public float MainRadiusZ;

        //字段职责：记录所有可作为兴趣点或资源散布中心的小洞室。
        public readonly List<IntVec3> SmallRooms = new List<IntVec3>();

        //字段职责：记录需要表现为塌方和碎石边缘的位置。
        public readonly List<IntVec3> Collapses = new List<IntVec3>();

        //字段职责：记录主拓扑通道中心线附近必须保持通行的格子。
        public readonly HashSet<IntVec3> ProtectedRouteCells = new HashSet<IntVec3>();

        //字段职责：记录蚁巢等完整场景保留区，阻止后续遗迹、装饰和植物覆盖。
        public readonly HashSet<IntVec3> ReservedSceneCells = new HashSet<IntVec3>();

        //字段职责：缓存每个地图格到最近洞壁的距离，数值大于五表示远离洞壁。
        public byte[] CaveEdgeDistances;

        //函数职责：按最小间距记录一个塌方中心，避免多个塌方重叠成同一片岩堆。
        public bool TryAddCollapse(IntVec3 cell, float minimumDistance)
        {
            for (int i = 0; i < Collapses.Count; i++)
            {
                if (cell.DistanceTo(Collapses[i]) < minimumDistance)
                {
                    return false;
                }
            }

            Collapses.Add(cell);
            return true;
        }
    }
}
