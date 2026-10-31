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

    //八方向水域距离只用于快速排除中心附近积水，完整轮廓另行检查。
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

    //近、远洞室仍使用原来的独立随机流和轮廓，不随候选尝试次数变化。
    private static (Shape Shape, double[] Knots, string Seed) RoomShape(CaveSettings settings, bool far)
    {
        string seed = settings.Seed + ":nest:" + (far ? "far" : "near");
        var random = Random(seed); double rx = settings.Size * (.041 + random() * .009), ry = rx * (.8 + random() * .35);
        var shape = new Shape(0, 0, rx, ry, random() * Math.PI);
        var knots = Enumerable.Range(0, 11).Select(_ => .78 + random() * .48).ToArray();
        return (shape, knots, seed);
    }

    //估计距离只排序；仅以确定不可能满足的直线下界和内接干地净空排除候选。
    private static List<(int Cell, double Score)> Candidates(byte[] cells, int start, CaveSettings settings,
        Shape shape, int[] clearance, int[] walking, int[] gap, int[] anchor, bool far, int farCell = -1, int separation = 0)
    {
        int n = settings.Size, extent = (int)Math.Ceiling(Math.Max(shape.Rx, shape.Ry) * 1.45), margin = extent + 3;
        int inner = (int)Math.Floor(Math.Min(shape.Rx, shape.Ry) * (.78 - .065 * CaveSettings.Roughness) / Math.Sqrt(2));
        var candidates = new List<(int Cell, double Score)>();
        for (int y = margin; y < n - margin; y++)
            for (int x = margin; x < n - margin; x++)
            {
                int i = y * n + x;
                if (cells[i] != 0 || anchor[i] < 0 || clearance[i] <= inner) continue;
                if (!far && Math.Abs(x - start % n) + Math.Abs(y - start / n) > n * .4) continue;
                if (farCell >= 0 && Length(x - farCell % n, y - farCell / n) < separation) continue;
                double estimated = walking[anchor[i]] + gap[i], target = (far ? .95 : .3) * n;
                double neck = extent * (far ? 1.6 : .85);
                candidates.Add((i, Math.Abs(estimated - target) * .65 + Math.Abs(gap[i] - neck) * 1.6));
            }
        return candidates.OrderBy(c => c.Score).ThenBy(c => c.Cell).ToList();
    }

    //完整洞室和弯折洞颈都不得覆盖水面，只记录本次需要开掘的岩体。
    private static (List<int> Floor, List<Point> Path, int[] Changed)? Prepare(int center, Shape shape, double[] knots,
        string seed, int[] parent, byte[] cells, CaveSettings settings, bool far)
    {
        var footprint = Footprint(center, shape, knots, Hash(seed), settings.Size);
        if (footprint.Any(i => cells[i] >= 2)) return null;
        var path = Connector(center, parent, settings, far);
        if (path.Count == 0) return null;
        var neck = Corridor(path, cells, settings.Size);
        if (neck == null) return null;
        neck.UnionWith(footprint);
        return (footprint, path, neck.Where(i => cells[i] == 0).ToArray());
    }

    //候选只写自己的岩体格，失败或取消时可以精确恢复。
    private static void SetCells(byte[] cells, int[] changed, byte value)
    {
        foreach (int i in changed) cells[i] = value;
    }

    //两处同时满足约束后才提交，生态读取顺序始终为近洞、远洞。
    private static void Commit(CavePlan plan, int center, Shape shape, List<int> floor, List<Point> path, int n)
    {
        plan.Rooms.Add(new Shape((center % n + .5) / n, (center / n + .5) / n,
            shape.Rx / n, shape.Ry / n, shape.Angle) { Nest = true });
        plan.EndRooms.Add(new CavePlan.EndRoom { Center = center, Floor = floor, Connector = path });
    }

    //先寻找受约束更多的远洞，再联合选择近洞；任一组合失败均恢复后继续搜索。
    internal static IEnumerable Carve(byte[] cells, int start, CavePlan plan, CaveSettings settings)
    {
        int n = settings.Size, farAttempts = 0, farValid = 0, nearAttempts = 0;
        var clearance = Clearance(cells, n);
        var near = RoomShape(settings, false); var far = RoomShape(settings, true);
        int separation = (int)Math.Ceiling(Math.Max(far.Shape.Rx, far.Shape.Ry) * 1.45) * 3;
        var walk = CaveRaster.Walking(cells, n, start);
        var links = Connections(cells, n, walk.Distance);
        double minimumFar = Math.Max(n * .7, Math.Ceiling(n * .25) * 3);
        bool committed = false;
        foreach (var farCandidate in Candidates(cells, start, settings, far.Shape, clearance,
            walk.Distance, links.Gap, links.Anchor, true))
        {
            if (++farAttempts % 8 == 0) yield return null;
            var farPrepared = Prepare(farCandidate.Cell, far.Shape, far.Knots, far.Seed, links.Parent, cells, settings, true);
            if (!farPrepared.HasValue) continue;
            var farRoom = farPrepared.Value;
            SetCells(cells, farRoom.Changed, 1);
            try
            {
                var farWalk = CaveRaster.Walking(cells, n, start);
                //新增近洞只可能缩短已有路线，不足最低远洞距离的候选无需继续配对。
                if (farWalk.Distance[farCandidate.Cell] < minimumFar) continue;
                farValid++;
                var nearLinks = Connections(cells, n, farWalk.Distance);
                foreach (var nearCandidate in Candidates(cells, start, settings, near.Shape, clearance,
                    farWalk.Distance, nearLinks.Gap, nearLinks.Anchor, false, farCandidate.Cell, separation))
                {
                    if (++nearAttempts % 8 == 0) yield return null;
                    var nearPrepared = Prepare(nearCandidate.Cell, near.Shape, near.Knots, near.Seed,
                        nearLinks.Parent, cells, settings, false);
                    if (!nearPrepared.HasValue) continue;
                    var nearRoom = nearPrepared.Value;
                    SetCells(cells, nearRoom.Changed, 1);
                    try
                    {
                        var check = CaveRaster.Walking(cells, n, start);
                        int nearDistance = check.Distance[nearCandidate.Cell], farDistance = check.Distance[farCandidate.Cell];
                        if (nearDistance < n * .25 || nearDistance > n * .4
                            || farDistance < Math.Max(n * .7, nearDistance * 3)) continue;
                        Commit(plan, nearCandidate.Cell, near.Shape, nearRoom.Floor, nearRoom.Path, n);
                        Commit(plan, farCandidate.Cell, far.Shape, farRoom.Floor, farRoom.Path, n);
                        committed = true;
                        yield break;
                    }
                    finally { if (!committed) SetCells(cells, nearRoom.Changed, 0); }
                }
            }
            finally { if (!committed) SetCells(cells, farRoom.Changed, 0); }
        }
        throw new InvalidOperationException($"无法找到满足干燥与步行距离的近远洞室组合：模板 {settings.Template}，种子 {settings.Seed}，"
            + $"尺寸 {n}；远洞候选 {farAttempts}，达标远洞 {farValid}，近洞组合 {nearAttempts}。"
            + $"近洞要求 {n * .25:0.#}～{n * .4:0.#} 格，远洞至少 {minimumFar:0.#} 格且不少于近洞三倍。");
    }
}
