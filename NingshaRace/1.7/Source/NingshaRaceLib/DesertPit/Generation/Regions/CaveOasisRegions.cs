using System;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;
using Shape = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Shape;

namespace NingshaRaceLib.DesertPit.Generation.Regions;

//沿出生点附近的水源铺出绿洲腹地、湿润岸带和疏草边缘。
internal static class CaveOasisRegions
{
    //形状沿实际地面与浅水扩展，不能穿透岩壁或进入蚁巢。
    internal static byte[] Build(CaveEcologyLayout layout)
    {
        int n = layout.Size, start = layout.Start; var cells = layout.Cells;
        uint seed = Hash(layout.Settings.Seed + ":oasis"); var random = Random(layout.Settings.Seed + ":oasis-shape");
        var proximity = CaveEcologyLayout.Proximity(n, i => cells[i] >= 2);
        int water = proximity.Source[start];
        var spawn = new Point(start % n + .5, start / n + .5);
        var shore = new Point(water % n + .5, water / n + .5);
        double reach = Math.Min(1, n * .24 / Math.Max(1, Distance(spawn, shore)));
        var target = new Point(Lerp(spawn.X, shore.X, reach), Lerp(spawn.Y, shore.Y, reach));
        double radius = n * .155, angle = Math.Atan2(target.Y - spawn.Y, target.X - spawn.X) + (random() - .5) * .65;
        var basis = new Shape(Lerp(spawn.X, target.X, .18), Lerp(spawn.Y, target.Y, .18),
            radius * (1.04 + random() * .2), radius * (.78 + random() * .2), angle);
        var bank = new Shape(Lerp(spawn.X, target.X, .78), Lerp(spawn.Y, target.Y, .78), radius * .72, radius * .5, angle + Math.PI * .5);
        var reserved = CaveEcologyLayout.Proximity(n, i => layout.Reserved[i] != 0).Distance;
        var field = new float[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            double u = i % n / (double)n, v = i / n / (double)n;
            var p = new Point(i % n + .5 + Noise(u * 9, v * 9, seed) * radius * .17,
                i / n + .5 + Noise(u * 9, v * 9, seed + 31) * radius * .17);
            field[i] = (float)(-SmoothMin(Ellipse(p, basis), Ellipse(p, bank), radius * .2)
                + radius * (.13 * Noise(u * 17, v * 17, seed + 63) + .04 * Noise(u * 43, v * 43, seed + 97)));
        }
        field[start] = Math.Max(field[start], (float)(radius * .65));
        var zones = new byte[cells.Length]; var seen = new bool[cells.Length]; var queue = new int[cells.Length];
        int head = 0, tail = 1; queue[0] = start; seen[start] = true;
        while (head < tail)
        {
            int i = queue[head++]; double u = i % n / (double)n, v = i / n / (double)n;
            if (cells[i] == 1)
            {
                double edge = radius * (.2 + .18 * Noise(u * 13, v * 13, seed + 141));
                bool moist = proximity.Distance[i] < n * (.022 + .009 * Noise(u * 21, v * 21, seed + 199));
                zones[i] = (byte)(moist && field[i] > radius * .06 ? 3 : field[i] > edge ? 2 : 1);
            }
            foreach (int j in CaveRaster.Neighbors(i, n))
            {
                if (seen[j] || reserved[j] <= 2 || field[j] < 0 || layout.Walking[j] < 0 || cells[j] is not (1 or 2)) continue;
                seen[j] = true; queue[tail++] = j;
            }
        }
        return zones;
    }
}
