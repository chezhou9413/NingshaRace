using System;
using System.Linq;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Shape = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Shape;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//先布局主洞和湖盆，再进行整体变形；模板不保存固定像素地图。
internal static class CaveTemplates
{
    //使用实验台原始预设和阶段随机流构造主轮廓。
    internal static CavePlan Create(CaveSettings settings)
    {
        var plan = new CavePlan();
        var random = Random(settings.Seed + ":macro");
        void Room(double x, double y, double rx, double ry) =>
            plan.Rooms.Add(new Shape(x + (random() - .5) * .065, y + (random() - .5) * .065,
                rx, ry, (random() - .5) * .7) { Main = true });
        if (settings.Template == "lake")
            LakeBasin(plan, settings);
        else if (settings.Template == "confluence")
        {
            Room(.43, .52, .135, .13); Room(.63, .57, .11, .13); Room(.26, .57, .085, .085);
            Room(.65, .4, .075, .052); Room(.22, .35, .07, .065); Room(.45, .88, .11, .055);
            Room(.82, .74, .085, .075); plan.Spawn = plan.Rooms[1];
        }
        else
        {
            Room(.62, .44, .12, .135); Room(.48, .29, .085, .075); Room(.29, .18, .105, .08);
            Room(.22, .6, .1, .075); Room(.55, .74, .105, .075); Room(.82, .63, .075, .075);
            plan.Spawn = plan.Rooms[0];
        }
        Vary(plan, settings);
        return plan;
    }

    //围绕真实湖岸净空放置主洞，使入口不会落入湖中。
    private static void LakeBasin(CavePlan plan, CaveSettings settings)
    {
        var random = Random(settings.Seed + ":basin");
        double rx = .17 + random() * .055, ry = .17 + random() * .065, angle = random() * Math.PI;
        double margin = Math.Max(rx, ry) * 1.25 + .035;
        var lake = new Shape(Lerp(margin, 1 - margin, random()), Lerp(margin, 1 - margin, random()), rx, ry, angle);
        plan.Lakes.Add(lake);
        double lobeAngle = random() * Math.PI * 2;
        double reach = .65 / Length(Math.Cos(lobeAngle - angle) / rx, Math.Sin(lobeAngle - angle) / ry);
        plan.Lakes.Add(new Shape(lake.X + Math.Cos(lobeAngle) * reach, lake.Y + Math.Sin(lobeAngle) * reach,
            rx * (.45 + random() * .15), ry * (.4 + random() * .2), angle + random() - .5));
        int count = 4 + (int)(random() * 3);
        for (int attempt = 0; plan.Rooms.Count < count && attempt < 160; attempt++)
        {
            double roomRx = .07 + random() * .055, roomRy = .065 + random() * .05;
            double inset = Math.Max(roomRx, roomRy) * 1.2 + .025;
            var room = new Shape(Lerp(inset, 1 - inset, random()), Lerp(inset, 1 - inset, random()), roomRx, roomRy) { Main = true };
            double gap = plan.Lakes.Min(l => Ellipse(room, l));
            if (gap < Math.Min(roomRx, roomRy) * .55 + .025 || gap > .25) continue;
            if (plan.Rooms.Any(r => Distance(room, r) < Math.Max(roomRx, roomRy) + Math.Max(r.Rx, r.Ry) * .45)) continue;
            room.Angle = random() * Math.PI;
            plan.Rooms.Add(room);
        }
        if (plan.Rooms.Count == 0) throw new InvalidOperationException("湖盆没有入口洞室净空。");
        plan.Spawn = plan.Rooms[0];
    }

    //对共享形状只变换一次，通过协方差求出变形后的椭圆轴。
    private static void Vary(CavePlan plan, CaveSettings settings)
    {
        var random = Random(settings.Seed + ":layout-frame");
        int turns = (int)(random() * 4); bool mirror = random() < .5;
        double strength = settings.Template == "lake" ? 0 : 1;
        double ax = (.08 + random() * .07) * (random() < .5 ? -1 : 1) * strength;
        double ay = (.08 + random() * .07) * (random() < .5 ? -1 : 1) * strength;
        foreach (var p in plan.Rooms.Concat(plan.Lakes))
        {
            double x = p.X, y = p.Y;
            double sx = 1 + ax * Math.PI * Math.Cos(Math.PI * x), sy = 1 + ay * Math.PI * Math.Cos(Math.PI * y);
            p.X = x + ax * Math.Sin(Math.PI * x); p.Y = y + ay * Math.Sin(Math.PI * y);
            double c = Math.Cos(p.Angle), s = Math.Sin(p.Angle);
            double a = sx * sx * (p.Rx * p.Rx * c * c + p.Ry * p.Ry * s * s);
            double b = sx * sy * (p.Rx * p.Rx - p.Ry * p.Ry) * c * s;
            double d = sy * sy * (p.Rx * p.Rx * s * s + p.Ry * p.Ry * c * c), delta = Length(a - d, 2 * b);
            p.Rx = Math.Sqrt((a + d + delta) * .5); p.Ry = Math.Sqrt((a + d - delta) * .5);
            p.Angle = Math.Atan2(2 * b, a - d) * .5;
            if (mirror) { p.Angle = Math.PI - p.Angle; p.X = 1 - p.X; }
            p.Angle += turns * Math.PI * .5;
            for (int i = 0; i < turns; i++) (p.X, p.Y) = (1 - p.Y, p.X);
        }
    }
}
