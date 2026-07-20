using Verse;

namespace NingshaRaceLib.DesertPit
{
    //类职责：记录沙漠巨坑洞室拓扑节点的位置、规模和层级。
    internal class DesertPitCaveNode
    {
        //字段职责：记录洞室中心格。
        public IntVec3 Center;

        //字段职责：记录洞室基础半径。
        public float Radius;

        //字段职责：记录洞室是否是主洞室。
        public bool Main;

        //字段职责：记录洞室到主洞室的层级距离。
        public int Depth;
    }
}
