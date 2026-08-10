using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Caves
{
    //类职责：记录沙漠巨坑洞室拓扑节点的位置、规模和层级。
    internal class DesertPitCaveNode
    {
        //字段职责：记录洞室中心格。
        public IntVec3 Center;

        //字段职责：记录洞室局部横轴半径。
        public float RadiusX;

        //字段职责：记录洞室局部纵轴半径。
        public float RadiusZ;

        //字段职责：记录洞室椭圆相对地图坐标的旋转角度。
        public float Rotation;

        //字段职责：记录洞室是否是主洞室。
        public bool Main;

        //字段职责：记录洞室到主洞室的层级距离。
        public int Depth;

        //字段职责：记录拓扑中直接连接的父洞室，避免雕刻阶段重新猜测连接关系。
        public DesertPitCaveNode Parent;

        //属性职责：取得洞室用于边界与间距计算的最大半径。
        public float MaxRadius => Mathf.Max(RadiusX, RadiusZ);
    }
}
