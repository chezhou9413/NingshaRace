using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;
using Shape = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Shape;

namespace NingshaRaceLib.DesertPit.Generation.Regions;

//沿主河、相接入流、水潭及绿洲中心生成连续亮区。
internal static class CaveLightRegions
{
    //绿洲中央的受光核心与绿洲轮廓相交，不能越过岩壁和巢室。
    private static void OasisCore(CaveEcologyLayout layout, byte[] zones)
    {
        int n = layout.Size, center = layout.Start, cx = center % n, cy = center / n;
        string key = layout.Settings.Seed + ":light-shape:" + center;
        var random = Random(key); uint seed = Hash(key);
        double radius = n * .078, angle = random() * Math.PI * 2;
        var basis = new Shape(cx + .5, cy + .5, radius * (.9 + random() * .25), radius * (.68 + random() * .25), angle);
        var lobe = new Shape(cx + .5 + Math.Cos(angle) * radius * .55, cy + .5 + Math.Sin(angle) * radius * .55,
            radius * .72, radius * (.45 + random() * .2), angle + random() - .5);
        int extent = (int)Math.Ceiling(radius * 1.9);
        var field = new float[zones.Length]; Array.Fill(field, float.NegativeInfinity);
        for (int y = Math.Max(0, cy - extent); y <= Math.Min(n - 1, cy + extent); y++)
            for (int x = Math.Max(0, cx - extent); x <= Math.Min(n - 1, cx + extent); x++)
            {
                int i = y * n + x;
                if (layout.Cells[i] == 0 || layout.Reserved[i] != 0 || layout.Cells[i] == 1 && layout.Oasis[i] == 0) continue;
                double u = (x - cx) / radius, v = (y - cy) / radius;
                var p = new Point(x + .5 + Noise(u * 2, v * 2, seed) * radius * .16,
                    y + .5 + Noise(u * 2, v * 2, seed + 37) * radius * .16);
                field[i] = (float)(-SmoothMin(Ellipse(p, basis), Ellipse(p, lobe), radius * .22)
                    + radius * (.12 * Noise(u * 3, v * 3, seed + 73) + .04 * Noise(u * 8, v * 8, seed + 101)));
            }
        field[center] = Math.Max(field[center], (float)(radius * .45));
        var seen = new bool[zones.Length]; var queue = new Queue<int>(); queue.Enqueue(center); seen[center] = true;
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            if (layout.Cells[i] == 1 && layout.Walking[i] >= 0) zones[i] = (byte)(field[i] > radius * .22 ? 2 : 1);
            foreach (int j in CaveRaster.Neighbors(i, n))
                if (!seen[j] && field[j] >= 0) { seen[j] = true; queue.Enqueue(j); }
        }
    }

    //用河道左右宽度收集一段连续水面。
    internal static void RiverSources(byte[] sources, List<Point> path, double end, CaveEcologyLayout layout, double begin = 0)
    {
        int n = layout.Size;
        foreach (var p in path)
        {
            if (p.S > end) break;
            if (p.S < begin) continue;
            double offset = (p.Left - p.Right) * .5, r = (p.Left + p.Right) * n * .5 + 2;
            double cx = (p.X + p.Nx * offset) * n - .5, cy = (p.Y + p.Ny * offset) * n - .5;
            for (int y = Math.Max(0, (int)Math.Floor(cy - r)); y <= Math.Min(n - 1, (int)Math.Ceiling(cy + r)); y++)
                for (int x = Math.Max(0, (int)Math.Floor(cx - r)); x <= Math.Min(n - 1, (int)Math.Ceiling(cx + r)); x++)
                    if (layout.Cells[y * n + x] >= 2 && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) sources[y * n + x] = 1;
        }
    }

    //保留水潭不规则岸线，光源只从已有水格开始。
    internal static void PoolSources(byte[] sources, Shape pool, CaveEcologyLayout layout)
    {
        int n = layout.Size;
        double padding = Math.Max(pool.Rx, pool.Ry) * .45 + CaveSettings.Roughness * .026;
        double extent = Math.Max(pool.Rx, pool.Ry) + padding;
        for (int y = Math.Max(0, (int)Math.Floor((pool.Y - extent) * n)); y <= Math.Min(n - 1, (int)Math.Ceiling((pool.Y + extent) * n)); y++)
            for (int x = Math.Max(0, (int)Math.Floor((pool.X - extent) * n)); x <= Math.Min(n - 1, (int)Math.Ceiling((pool.X + extent) * n)); x++)
                if (layout.Cells[y * n + x] >= 2 && Ellipse(new Point((x + .5) / n, (y + .5) / n), pool) <= padding) sources[y * n + x] = 1;
    }

    //沿可通行洞穴扩散明暗带，噪声改变宽度但不穿透岩壁。
    internal static void Spread(byte[] sources, byte[] zones, CaveEcologyLayout layout, bool dim)
    {
        int n = layout.Size;
        var widths = new float[zones.Length]; var distance = new int[zones.Length]; Array.Fill(distance, -1);
        var queue = new int[zones.Length]; int head = 0, tail = 0;
        uint seed = Hash(layout.Settings.Seed + (dim ? ":dim-transition" : ":shore-light"));
        for (int i = 0; i < zones.Length; i++)
        {
            double x = i % n / (double)n, y = i / n / (double)n, reach = Math.Max(0, Noise(x * 6, y * 6, seed + 71));
            widths[i] = (float)(dim ? Math.Max(2, n * .1 * (.9 + .48 * Noise(x * 11, y * 11, seed)
                + .16 * Noise(x * 29, y * 29, seed + 37) + reach * reach * .8)) :
                n * .05 * (.9 + .3 * Noise(x * 9, y * 9, seed) + .13 * Noise(x * 27, y * 27, seed + 43)));
            if (sources[i] != 0 || dim && zones[i] is 1 or 2) { distance[i] = 0; queue[tail++] = i; }
        }
        while (head < tail)
        {
            int i = queue[head++];
            if (layout.Cells[i] == 1 && layout.Walking[i] >= 0 && layout.Reserved[i] == 0)
            {
                if (dim && zones[i] == 0) zones[i] = (byte)(distance[i] < widths[i] * .62 ? 3 : 4);
                if (!dim) zones[i] = Math.Max(zones[i], (byte)(distance[i] < widths[i] * .75 ? 2 : 1));
            }
            foreach (int j in CaveRaster.Neighbors(i, n))
            {
                if (distance[j] >= 0 || layout.Cells[j] == 0 || layout.Reserved[j] != 0 || distance[i] + 1 > widths[j]) continue;
                distance[j] = distance[i] + 1; queue[tail++] = j;
            }
        }
    }

    //使用实验台默认的百分之五十五主河受光比例、半数大水潭和三处独立灰暗区。
    internal static byte[] Build(CaveEcologyLayout layout)
    {
        var zones = new byte[layout.Cells.Length]; var sources = new byte[zones.Length];
        OasisCore(layout, zones);
        var main = layout.Plan.Rivers[0]; var inside = main.Where(p => p.X >= 0 && p.Y >= 0 && p.X < 1 && p.Y < 1).ToList();
        RiverSources(sources, main, Lerp(inside[0].S, inside[^1].S, .55), layout);
        foreach (var inlet in layout.Plan.Inlets)
        {
            var join = inlet[^1]; int i = (int)(join.Y * layout.Size) * layout.Size + (int)(join.X * layout.Size);
            if (sources[i] != 0) RiverSources(sources, inlet, double.PositiveInfinity, layout);
        }
        foreach (var pool in layout.Plan.Pools.OrderByDescending(p => p.Rx * p.Ry).Take((layout.Plan.Pools.Count + 1) / 2))
            PoolSources(sources, pool, layout);
        Spread(sources, zones, layout, false);
        Spread(CaveDimRegions.Sources(layout, zones), zones, layout, true);
        return zones;
    }
}
