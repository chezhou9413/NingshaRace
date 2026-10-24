using System;
using System.Collections.Generic;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//复现实验台的字符串散列、值噪声和归一化几何运算。
internal static class CaveMath
{
    internal static double Lerp(double a, double b, double t) => a + (b - a) * t;
    internal static double Smooth(double t) => t * t * (3 - 2 * t);
    internal static double Length(double x, double y) => Math.Sqrt(x * x + y * y);
    internal static double Distance(CavePlan.Point a, CavePlan.Point b) => Length(a.X - b.X, a.Y - b.Y);

    //阶段名称隔离随机流，保持各阶段的候选序列稳定。
    internal static uint Hash(string text)
    {
        uint h = 2166136261;
        foreach (char c in text) h = unchecked((h ^ c) * 16777619);
        return h;
    }

    //与实验台的 Mulberry32 使用相同的无符号溢出语义。
    internal static Func<double> Random(string seed)
    {
        uint state = Hash(seed);
        return () =>
        {
            unchecked
            {
                state += 0x6d2b79f5;
                uint t = (state ^ (state >> 15)) * (1 | state);
                t ^= t + (t ^ (t >> 7)) * (61 | t);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        };
    }

    //平滑值噪声只改变轮廓，不改变洞室连接关系。
    internal static double Noise(double x, double y, uint seed)
    {
        int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
        double Sample(int a, int b)
        {
            unchecked
            {
                uint h = (uint)(a * 374761393 + b * 668265263) + seed;
                h = (h ^ (h >> 13)) * 1274126177;
                return (h ^ (h >> 16)) / 4294967295.0;
            }
        }
        double u = Smooth(x - ix), v = Smooth(y - iy);
        return Lerp(Lerp(Sample(ix, iy), Sample(ix + 1, iy), u),
            Lerp(Sample(ix, iy + 1), Sample(ix + 1, iy + 1), u), v) * 2 - 1;
    }

    //以有界平滑并集合并洞壁。
    internal static double SmoothMin(double a, double b, double k)
    {
        if (k <= 0) return Math.Min(a, b);
        double h = Math.Max(k - Math.Abs(a - b), 0) / k;
        return Math.Min(a, b) - h * h * k * .25;
    }

    //旋转椭圆的隐式距离用于轮廓栅格化。
    internal static double Ellipse(CavePlan.Point p, CavePlan.Shape e)
    {
        double c = Math.Cos(e.Angle), s = Math.Sin(e.Angle), dx = p.X - e.X, dy = p.Y - e.Y;
        return (Length((dx * c + dy * s) / e.Rx, (-dx * s + dy * c) / e.Ry) - 1) * Math.Min(e.Rx, e.Ry);
    }

    //计算到有限线段的距离。
    internal static double Segment(CavePlan.Point p, CavePlan.Point a, CavePlan.Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, length = dx * dx + dy * dy;
        double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (length == 0 ? 1 : length), 0, 1);
        return Length(p.X - a.X - dx * t, p.Y - a.Y - dy * t);
    }

    //按段长采样 Catmull-Rom 曲线，保留原锚点的参数位置。
    internal static List<CavePlan.Point> Spline(IReadOnlyList<CavePlan.Point> points, int density)
    {
        var result = new List<CavePlan.Point>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            var a = points[Math.Max(0, i - 1)]; var b = points[i];
            var c = points[i + 1]; var d = points[Math.Min(points.Count - 1, i + 2)];
            int steps = Math.Max(3, (int)Math.Ceiling(Distance(b, c) * density));
            for (int j = 0; j < steps; j++)
            {
                double t = (double)j / steps, t2 = t * t, t3 = t2 * t;
                double Interpolate(double aa, double bb, double cc, double dd) =>
                    .5 * (2 * bb + (-aa + cc) * t + (2 * aa - 5 * bb + 4 * cc - dd) * t2 + (-aa + 3 * bb - 3 * cc + dd) * t3);
                result.Add(new CavePlan.Point(Interpolate(a.X, b.X, c.X, d.X), Interpolate(a.Y, b.Y, c.Y, d.Y))
                    { T = (i + t) / (points.Count - 1) });
            }
        }
        result.Add(new CavePlan.Point(points[^1].X, points[^1].Y) { T = 1 });
        return result;
    }
}
