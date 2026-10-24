using System;
using System.Collections.Generic;
using System.Linq;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;
using Shape = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Shape;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//根据水面距离布置支洞，用带穿水代价的连接树保持主结构连通。
internal static class CaveConnections
{
    //河流宽度参与距离判断，避免把河岸误判为空地。
    internal static double WaterDistance(Point p, CavePlan plan)
    {
        double distance = 1;
        foreach (var lake in plan.Lakes) distance = Math.Min(distance, Ellipse(p, lake));
        foreach (var pool in plan.Pools) distance = Math.Min(distance, Ellipse(p, pool));
        foreach (var path in plan.Rivers)
            for (int j = 0; j < path.Count - 1; j += 6)
            {
                var a = path[j]; var b = path[Math.Min(j + 6, path.Count - 1)];
                double width = Math.Max(Math.Max(a.Left, a.Right), Math.Max(b.Left, b.Right));
                distance = Math.Min(distance, Segment(p, a, b) - width);
            }
        return distance;
    }

    //两个末端空洞在栅格化之后定位，普通支洞预留其数量。
    internal static void AddRooms(CavePlan plan, CaveSettings settings)
    {
        var random = Random(settings.Seed + ":rooms");
        for (int attempt = 0; plan.Rooms.Count < settings.Chambers - 2 && attempt < 1400; attempt++)
        {
            var p = new Point(.06 + random() * .88, .06 + random() * .88);
            double radius = .018 + Math.Pow(random(), 1.7) * .037;
            if (WaterDistance(p, plan) < radius + .014) continue;
            double gap = plan.Rooms.Min(r => Ellipse(p, r));
            if (gap < radius + .02 || gap > .23) continue;
            plan.Rooms.Add(new Shape(p.X, p.Y, radius, radius * (.65 + random() * .55), random() * Math.PI));
        }
    }

    //先连接各洞室，再对短边抽取少量回路并寻找较少穿水的曲线。
    internal static void Connect(CavePlan plan, CaveSettings settings)
    {
        var random = Random(settings.Seed + ":links");
        int count = plan.Rooms.Count;
        var candidates = new List<(int A, int B, double Distance, double Cost)>();
        for (int a = 0; a < count; a++)
            for (int b = a + 1; b < count; b++)
            {
                var from = plan.Rooms[a]; var to = plan.Rooms[b]; int wet = 0;
                double distance = Distance(from, to);
                for (int j = 1; j < 9; j++)
                    if (WaterDistance(new Point(Lerp(from.X, to.X, j / 9.0), Lerp(from.Y, to.Y, j / 9.0)), plan) < 0) wet++;
                candidates.Add((a, b, distance, distance * (1 + wet * .55)));
            }
        int[] parent = Enumerable.Range(0, count).ToArray();
        int Root(int i) { while (parent[i] != i) i = parent[i]; return i; }
        var edges = new List<(int A, int B)>();
        foreach (var edge in candidates.OrderBy(e => e.Cost))
        {
            int a = Root(edge.A), b = Root(edge.B);
            if (a != b) { parent[a] = b; edges.Add((edge.A, edge.B)); }
            else if (edge.Distance < .24 && random() < settings.Loops * .16) edges.Add((edge.A, edge.B));
        }
        foreach (var edge in edges)
        {
            var from = plan.Rooms[edge.A]; var to = plan.Rooms[edge.B];
            double length = Distance(from, to), nx = -(to.Y - from.Y) / length, ny = (to.X - from.X) / length;
            double preferred = (.12 + random() * .3) * (random() < .5 ? -1 : 1) * settings.Meander;
            string seed = settings.Seed + ":tunnel:" + edge.A + ":" + edge.B;
            List<Point> best = null; double score = double.PositiveInfinity, margin = 3.0 / settings.Size;
            foreach (double bend in new[] { preferred, -preferred, preferred * 1.6, -preferred * 1.6, 0 })
            {
                var control = new Point((from.X + to.X) * .5 + nx * length * bend, (from.Y + to.Y) * .5 + ny * length * bend);
                var path = CaveChannels.Natural(new Point[] { from, control, to }, seed, false, settings);
                if (path.Any(p => p.X < margin || p.Y < margin || p.X > 1 - margin || p.Y > 1 - margin)) continue;
                double cost = Math.Abs(bend - preferred) * .5;
                for (int j = 0; j < path.Count; j += 5) if (WaterDistance(path[j], plan) < 0) cost++;
                if (cost < score) { score = cost; best = path; }
            }
            if (best == null) throw new InvalidOperationException("洞道弯折超出地图边界。");
            plan.Tunnels.Add(CaveChannels.Profile(best, seed, false, settings));
        }
    }
}
