using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;
using Shape = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Shape;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//保留实验台近、远末端洞室的几何约束，记录供后续生态使用的实际地面。
internal static class CaveEndRooms
{
    //从可达干地向岩体扩展，记录最短开掘路线及其原有接入口。
    private static (int[] Gap, int[] Anchor, int[] Parent) Connections(byte[] cells, int n, int[] walking)
    {
        var gap = new int[cells.Length]; var anchor = new int[cells.Length]; var parent = new int[cells.Length];
        var queue = new int[cells.Length]; int head = 0, tail = 0;
        Array.Fill(gap, -1); Array.Fill(anchor, -1); Array.Fill(parent, -1);
        for (int i = 0; i < cells.Length; i++)
            if (cells[i] == 1 && walking[i] >= 0) { gap[i] = 0; anchor[i] = i; queue[tail++] = i; }
        while (head < tail)
        {
            int i = queue[head++];
            foreach (int j in CaveRaster.Neighbors(i, n))
            {
                if (gap[j] >= 0 || cells[j] >= 2) continue;
                gap[j] = gap[i] + 1; anchor[j] = anchor[i]; parent[j] = i; queue[tail++] = j;
            }
        }
        return (gap, anchor, parent);
    }

    //八方向水域距离保守估计完整干地洞室所需的净空。
    private static int[] Clearance(byte[] cells, int n)
    {
        var distance = new int[cells.Length]; var queue = new int[cells.Length]; int head = 0, tail = 0;
        Array.Fill(distance, n * 2);
        for (int i = 0; i < cells.Length; i++)
            if (cells[i] >= 2) { distance[i] = 0; queue[tail++] = i; }
        while (head < tail)
        {
            int i = queue[head++], x = i % n, y = i / n;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int xx = x + dx, yy = y + dy, j = yy * n + xx;
                    if (xx < 0 || yy < 0 || xx >= n || yy >= n || distance[j] <= distance[i] + 1) continue;
                    distance[j] = distance[i] + 1; queue[tail++] = j;
                }
        }
        return distance;
    }

    //末端洞室使用不等半径的闭合轮廓，候选中心已预留完整边界。
    private static List<int> Footprint(int center, Shape shape, double[] knots, uint seed, int n)
    {
        int cx = center % n, cy = center / n, extent = (int)Math.Ceiling(Math.Max(shape.Rx, shape.Ry) * 1.45);
        var cells = new List<int>(); double c = Math.Cos(shape.Angle), s = Math.Sin(shape.Angle);
        for (int y = cy - extent; y <= cy + extent; y++)
            for (int x = cx - extent; x <= cx + extent; x++)
            {
                double dx = x - cx, dy = y - cy, u = (dx * c + dy * s) / shape.Rx, v = (-dx * s + dy * c) / shape.Ry;
                double a = (Math.Atan2(v, u) + Math.PI) * knots.Length / (Math.PI * 2); int k = (int)Math.Floor(a);
                double edge = Lerp(knots[k % knots.Length], knots[(k + 1) % knots.Length], Smooth(a - k));
                double rough = Noise(x * .17, y * .17, seed) * .065 * CaveSettings.Roughness;
                if (Length(u, v) <= edge + rough) cells.Add(y * n + x);
            }
        return cells;
    }

    //近洞使用较短接入线，远洞加入两次错开的弯折。
    private static List<Point> Connector(int center, int[] parent, CaveSettings settings, bool far)
    {
        int n = settings.Size; var trace = new List<Point>();
        for (int i = center; i >= 0; i = parent[i]) trace.Add(new Point((i % n + .5) / n, (i / n + .5) / n));
        if (trace.Count < 2) return new List<Point>();
        trace.Reverse();
        string seed = settings.Seed + ":nest-neck:" + (far ? "far" : "near");
        var config = new CaveSettings(settings.Seed, n, settings.Template)
            { TunnelWidth = Math.Min(4, settings.TunnelWidth), Meander = Math.Min(1, settings.Meander) };
        var anchors = trace.Where((_, i) => i == 0 || i == trace.Count - 1 || i % 5 == 0).ToList();
        if (far)
        {
            var random = Random(seed + ":turns"); var first = trace[0]; var last = trace[^1];
            double angle = Math.Atan2(last.Y - first.Y, last.X - first.X), side = random() < .5 ? -1 : 1;
            double amplitude = Math.Min((trace.Count - 1.0) / n * .24, .026) * config.Meander;
            var turns = new[] { (T: .24 + random() * .08, Offset: side * amplitude * (.85 + random() * .3)),
                (T: .56 + random() * .1, Offset: -side * amplitude * (.55 + random() * .3)) };
            anchors = new List<Point> { first };
            foreach (var turn in turns)
            {
                var p = trace[(int)Math.Floor(turn.T * (trace.Count - 1) + .5)];
                anchors.Add(new Point(p.X - Math.Sin(angle) * turn.Offset, p.Y + Math.Cos(angle) * turn.Offset));
            }
            anchors.Add(last);
        }
        return CaveChannels.Profile(CaveChannels.Natural(anchors, seed, false, config), seed, false, config);
    }

    //连接洞颈不得覆盖水面，非法候选直接舍弃。
    private static HashSet<int> Corridor(List<Point> path, byte[] cells, int n)
    {
        var indices = new HashSet<int>();
        foreach (var p in path)
        {
            double cx = p.X * n - .5, cy = p.Y * n - .5, r = Math.Max(p.Left, p.Right) * n;
            if (cx < 0 || cy < 0 || cx > n - 1 || cy > n - 1) return null;
            for (int y = Math.Max(0, (int)Math.Floor(cy - r)); y <= Math.Min(n - 1, (int)Math.Ceiling(cy + r)); y++)
                for (int x = Math.Max(0, (int)Math.Floor(cx - r)); x <= Math.Min(n - 1, (int)Math.Ceiling(cx + r)); x++)
                {
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r) continue;
                    int i = y * n + x;
                    if (cells[i] >= 2) return null;
                    indices.Add(i);
                }
        }
        return indices;
    }

    //使用实际可走路径验收两处末端空洞，失败时保留明确原因。
    internal static IEnumerable Carve(byte[] cells, int start, CavePlan plan, CaveSettings settings)
    {
        int n = settings.Size, nearCell = -1; var clearance = Clearance(cells, n);
        var chosenRooms = new List<Shape>();
        foreach (bool far in new[] { false, true })
        {
            string role = far ? "far" : "near", seed = settings.Seed + ":nest:" + role;
            var random = Random(seed); double rx = n * (.041 + random() * .009), ry = rx * (.8 + random() * .35);
            var shape = new Shape(0, 0, rx, ry, random() * Math.PI);
            var knots = Enumerable.Range(0, 11).Select(_ => .78 + random() * .48).ToArray();
            var walk = CaveRaster.Walking(cells, n, start);
            var links = Connections(cells, n, walk.Distance);
            int extent = (int)Math.Ceiling(Math.Max(rx, ry) * 1.45), margin = extent + 3;
            int step = Math.Max(2, (int)Math.Floor(n / 100.0 + .5));
            var candidates = new List<(int Cell, double Score)>();
            for (int y = margin; y < n - margin; y += step)
                for (int x = margin; x < n - margin; x += step)
                {
                    int i = y * n + x;
                    if (links.Anchor[i] < 0 || clearance[i] <= extent) continue;
                    double euclidean = Length(x - start % n, y - start / n), estimated = walk.Distance[links.Anchor[i]] + links.Gap[i];
                    if (!far && (euclidean < n * .22 || euclidean > n * .38 || estimated > n * .46)) continue;
                    if (far && (euclidean < n * .4 || estimated < n * .7)) continue;
                    if (chosenRooms.Exists(r => Length(x - r.X * n, y - r.Y * n) < extent * 3)) continue;
                    double target = (far ? .95 : .3) * n, neck = extent * (far ? 1.6 : .85);
                    candidates.Add((i, Math.Abs(estimated - target) * .65 + Math.Abs(links.Gap[i] - neck) * 1.6));
                }
            bool found = false; int attempts = 0;
            foreach (var candidate in candidates.OrderBy(c => c.Score))
            {
                var footprint = Footprint(candidate.Cell, shape, knots, Hash(seed), n);
                var path = Connector(candidate.Cell, links.Parent, settings, far);
                var neck = Corridor(path, cells, n);
                if (neck == null) continue;
                neck.UnionWith(footprint);
                var changed = neck.Where(i => cells[i] == 0).ToArray();
                foreach (int i in changed) cells[i] = 1;
                var check = CaveRaster.Walking(cells, n, start);
                int distance = check.Distance[candidate.Cell], nearDistance = far ? check.Distance[nearCell] : 0;
                bool valid = far ? distance >= Math.Max(n * .7, nearDistance * 3) && nearDistance >= n * .25 && nearDistance <= n * .4 :
                    distance >= n * .25 && distance <= n * .4;
                if (valid)
                {
                    var room = new Shape((candidate.Cell % n + .5) / n, (candidate.Cell / n + .5) / n, rx / n, ry / n, shape.Angle);
                    room.Nest = true;
                    chosenRooms.Add(room); plan.Rooms.Add(room);
                    plan.EndRooms.Add(new CavePlan.EndRoom { Center = candidate.Cell, Floor = footprint, Connector = path });
                    if (!far) nearCell = candidate.Cell;
                    found = true; break;
                }
                foreach (int i in changed) cells[i] = 0;
                //候选之间保持当前地图完整，便于生成进度窗口处理一帧。
                if (++attempts % 8 == 0) yield return null;
            }
            if (!found) throw new InvalidOperationException("当前地形无法同时容纳近、远两个干燥末端洞室。");
            yield return null;
        }
    }
}
