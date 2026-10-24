using System.Collections.Generic;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//保存一次洞穴基座生成的归一化形状，离开生成步骤后即可释放。
internal sealed class CavePlan
{
    internal readonly List<Shape> Rooms = new(), Lakes = new(), Pools = new(), Pillars = new();
    internal readonly List<Point> Ports = new();
    internal readonly List<List<Point>> Rivers = new(), Tunnels = new();
    internal readonly List<List<Point>> Inlets = new();
    internal readonly List<EndRoom> EndRooms = new();
    internal Point Spawn;

    //保留末端洞室的实际地面和洞颈，供生态避让和巢群落位。
    internal sealed class EndRoom
    {
        internal int Center;
        internal List<int> Floor;
        internal List<Point> Connector;
    }

    //点同时保存曲线参数和左右岸宽度，避免采样阶段反复分配附加对象。
    internal class Point
    {
        internal double X, Y, T, S, Angle, Left, Right, BankLeft, BankRight;
        internal double Nx => -System.Math.Sin(Angle);
        internal double Ny => System.Math.Cos(Angle);
        internal Point(double x, double y) { X = x; Y = y; }
    }

    //洞室、湖泊与岩柱共用椭圆轮廓；水坑额外关联其终点洞室。
    internal sealed class Shape : Point
    {
        internal double Rx, Ry;
        internal bool Main, Nest;
        internal Shape Room;
        internal string Seed;
        internal Shape(double x, double y, double rx, double ry, double angle = 0) : base(x, y)
        { Rx = rx; Ry = ry; Angle = angle; }
    }
}
