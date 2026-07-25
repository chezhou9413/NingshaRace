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

        //字段职责：记录主洞室横向半径，供后续步骤判断中心区域和边缘区域。
        public float MainRadiusX;

        //字段职责：记录主洞室纵向半径，供后续步骤判断中心区域和边缘区域。
        public float MainRadiusZ;

        //字段职责：记录所有可作为兴趣点或资源散布中心的小洞室。
        public readonly List<IntVec3> SmallRooms = new List<IntVec3>();

        //字段职责：记录需要表现为塌方和碎石边缘的位置。
        public readonly List<IntVec3> Collapses = new List<IntVec3>();
    }
}
