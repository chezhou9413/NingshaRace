using System;
using System.Collections.Generic;
using System.Linq;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveConnections;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;
using Shape = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Shape;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//先确定入流、汇流和分流终点，再生成河道宽度场与截流水坑。
internal static class CaveHydrology
{
    //边缘端口保持间隔，内侧锚点使进出地图的一段大致正交。
    private static (Point Outer, Point Inner) Port(CavePlan plan, int edge, Func<double> random)
    {
        Point At(double t) => edge switch { 0 => new(t, -.035), 1 => new(1.035, t), 2 => new(t, 1.035), _ => new(-.035, t) };
        var choices = Enumerable.Range(0, 16).Select(_ => .16 + random() * .68).ToArray();
        double Gap(double t) => plan.Ports.Aggregate(2.0, (gap, p) => Math.Min(gap, Distance(At(t), p)));
        var point = At(choices.OrderByDescending(Gap).First());
        plan.Ports.Add(point);
        return (point, new Point(Math.Clamp(point.X, .15, .85), Math.Clamp(point.Y, .15, .85)));
    }

    //中间控制点约束在地图内部。
    private static Point Bend(Point a, Point b, Func<double> random, double amount)
    {
        double k = (random() - .5) * amount;
        return new Point(Math.Clamp((a.X + b.X) * .5 - (b.Y - a.Y) * k, .12, .88),
            Math.Clamp((a.Y + b.Y) * .5 + (b.X - a.X) * k, .12, .88));
    }

    //按实际曲线弧长选择汇流点。
    private static Point RiverPoint(List<Point> path, double fraction) => path.First(p => p.S >= path[^1].S * fraction);

    //入流口逐渐收窄，左右岸保留独立变化。
    private static List<Point> Add(CavePlan plan, CaveSettings settings, IReadOnlyList<Point> anchors,
        double width, bool inlet = false, List<Point> path = null)
    {
        string seed = settings.Seed + ":flow:" + plan.Rivers.Count;
        path = CaveChannels.Profile(path ?? CaveChannels.Natural(anchors, seed, true, settings), seed, true, settings, width);
        if (inlet)
            foreach (var p in path)
            {
                double fade = 1 - Smooth(Math.Clamp((p.S - .035) / .18, 0, 1));
                double basis = settings.WaterWidth * width / (2 * settings.Size);
                p.Left = Lerp(p.Left, Math.Max(p.Left * 2.6, basis * 2.6), fade);
                p.Right = Lerp(p.Right, Math.Max(p.Right * 2.6, basis * 2.6), fade);
            }
        if (inlet && plan.Rivers.Count > 0) plan.Inlets.Add(path);
        plan.Rivers.Add(path);
        return path;
    }

    //选择远离水域的末端洞室，候选顺序与实验台一致。
    private static Shape Terminal(CavePlan plan, Point start, CaveSettings settings, Func<double> random)
    {
        var candidates = plan.Rooms.Select(r => (Room: r, Existing: true)).ToList();
        for (int i = 0; i < 64; i++)
        {
            double rx = .062 + random() * .025, ry = rx * (.7 + random() * .5);
            double margin = Math.Max(rx, ry) * 1.5 + .025;
            candidates.Add((new Shape(Lerp(margin, 1 - margin, random()), Lerp(margin, 1 - margin, random()),
                rx, ry, random() * Math.PI) { Main = true }, false));
        }
        Shape best = null; double cost = double.PositiveInfinity; bool existing = false;
        foreach (var item in candidates)
        {
            var room = item.Room; double distance = Distance(start, room);
            if (distance < .18 || distance > .65 || Math.Min(room.Rx, room.Ry) < .035) continue;
            if (WaterDistance(room, plan) < Math.Max(room.Rx, room.Ry) * .8 + .035 || Distance(room, plan.Spawn) < .1) continue;
            double score = Math.Abs(distance - .33) + (item.Existing ? 0 : .09);
            if (score < cost) { best = room; cost = score; existing = item.Existing; }
        }
        if (best == null) throw new InvalidOperationException("河湖之间没有截流洞室净空。");
        if (!existing) plan.Rooms.Add(best);
        return best;
    }

    //比较支流曲线，减少意外相交，同时禁止截流支流出界。
    private static List<Point> Branch(Point start, Point end, Point inner, CavePlan plan, CaveSettings settings, Func<double> random)
    {
        List<Point> best = null; int score = int.MaxValue;
        for (int i = 0; i < 10; i++)
        {
            var anchors = new List<Point> { start, Bend(start, inner ?? end, random, 1.1) };
            if (inner != null) anchors.Add(inner);
            anchors.Add(end);
            var path = CaveChannels.Natural(anchors, settings.Seed + ":flow:" + plan.Rivers.Count, true, settings);
            int wet = 0;
            for (int j = 0; j < path.Count; j += 6)
            {
                var p = path[j];
                if (Distance(start, p) < .065) continue;
                if (WaterDistance(p, plan) < .012) wet++;
                if (inner == null && (p.X < .035 || p.Y < .035 || p.X > .965 || p.Y > .965)) wet += 50;
            }
            if (wet < score) { best = path; score = wet; }
        }
        return best;
    }

    //自动数量使用独立随机流，主河道不会受支流数量影响。
    internal static void Build(CavePlan plan, CaveSettings settings)
    {
        var random = Random(settings.Seed + ":hydrology"); var counts = Random(settings.Seed + ":flow-counts");
        int inletCount = 1 + (int)(counts() * 2), branchCount = 1 + (int)(counts() * 2);
        int entryEdge = (int)(random() * 4);
        var inlet = Port(plan, entryEdge, random); var outlet = Port(plan, (entryEdge + 2) % 4, random);
        var hub = plan.Lakes.Count > 0 ? new Point(plan.Lakes[0].X, plan.Lakes[0].Y) :
            new Point(plan.Rooms.Sum(r => r.X / plan.Rooms.Count), plan.Rooms.Sum(r => r.Y / plan.Rooms.Count));
        var main = Add(plan, settings, new[] { inlet.Outer, inlet.Inner, Bend(inlet.Inner, hub, random, .75), hub,
            Bend(hub, outlet.Inner, random, .9), outlet.Inner, outlet.Outer }, 1.15, true);
        if (inletCount == 2)
        {
            var secondary = Random(settings.Seed + ":second-inlet");
            var source = Port(plan, (entryEdge + (secondary() < .5 ? 1 : 3)) % 4, secondary);
            var join = RiverPoint(main, .28 + secondary() * .15);
            Add(plan, settings, new[] { source.Outer, source.Inner, Bend(source.Inner, join, secondary, .6), join }, .8, true);
        }
        for (int i = 0; i < branchCount; i++)
        {
            var routeRandom = Random(settings.Seed + ":branch-route:" + i);
            double fraction = branchCount == 1 ? .4 + routeRandom() * .25 : .32 + i * .34 + routeRandom() * .08;
            var start = RiverPoint(main, fraction);
            if (plan.Lakes.Any(l => Ellipse(start, l) < .025))
            {
                var dry = main.Where(p => p.X > .13 && p.X < .87 && p.Y > .13 && p.Y < .87 && plan.Lakes.All(l => Ellipse(p, l) > .03))
                    .OrderBy(p => Math.Abs(p.S - start.S)).ToList();
                if (dry.Count == 0) throw new InvalidOperationException("巨湖覆盖了可用分流河段。");
                start = dry[0];
            }
            bool edgeEnd = Random(settings.Seed + ":branch-end:" + i)() < .5;
            if (edgeEnd)
            {
                var port = Port(plan, (entryEdge + (i == 0 ? 1 : 3)) % 4, routeRandom);
                Add(plan, settings, null, .68, path: Branch(start, port.Outer, port.Inner, plan, settings, routeRandom));
                continue;
            }
            var room = Terminal(plan, start, settings, routeRandom);
            double angle = Math.Atan2(room.Y - start.Y, room.X - start.X);
            var end = new Point(room.X + Math.Cos(angle) * room.Rx * .18, room.Y + Math.Sin(angle) * room.Ry * .18);
            var pool = new Shape(end.X, end.Y, room.Rx * (.45 + routeRandom() * .15), room.Ry * (.45 + routeRandom() * .18),
                room.Angle + (routeRandom() - .5) * .9) { Room = room, Seed = settings.Seed + ":pool:" + i };
            var branch = Add(plan, settings, null, .62, path: Branch(start, end, null, plan, settings, routeRandom));
            double total = branch[^1].S, neck = Math.Max(1.05 / settings.Size, Math.Min(pool.Rx, pool.Ry) * .25);
            foreach (var p in branch)
            {
                double fade = Math.Clamp((total - p.S) / .065, 0, 1);
                p.Left = Lerp(Math.Min(p.Left, neck), p.Left, fade); p.Right = Lerp(Math.Min(p.Right, neck), p.Right, fade);
            }
            plan.Pools.Add(pool);
        }
    }
}
