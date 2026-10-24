using System;
using System.Collections.Generic;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;
using Shape = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Shape;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//通过距离场并集生成洞壁，保护掩码保持离散通道连续。
internal sealed class CaveFields
{
    internal readonly int Size;
    internal readonly float[] Cave, Water, Wx, Wy, WallDetail;
    internal readonly byte[] Protected, WaterCore;
    internal const double Warp = CaveSettings.Roughness * .013;

    //缓存扭曲坐标，保持与实验台 Float32Array 相同的中间精度。
    internal CaveFields(CaveSettings settings)
    {
        Size = settings.Size;
        int count = Size * Size;
        Cave = new float[count]; Water = new float[count]; Wx = new float[count]; Wy = new float[count]; WallDetail = new float[count];
        Protected = new byte[count]; WaterCore = new byte[count];
        Array.Fill(Cave, 2); Array.Fill(Water, 2);
        uint seed = Hash(settings.Seed + ":boundary");
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int i = y * Size + x; double u = (x + .5) / Size, v = (y + .5) / Size;
                Wx[i] = (float)(u + (Noise(u * 9, v * 9, seed) + .3 * Noise(u * 31, v * 31, seed + 8)) * Warp);
                Wy[i] = (float)(v + (Noise(u * 9, v * 9, seed + 71) + .3 * Noise(u * 31, v * 31, seed + 91)) * Warp);
                WallDetail[i] = (float)(.65 * Noise(u * 29, v * 29, seed + 131) + .3 * Noise(u * 67, v * 67, seed + 173)
                    + .15 * Noise(u * 137, v * 137, seed + 211));
            }
    }

    //包围盒限定访问范围；仅洞室轮廓叠加多尺度岩齿。
    private void DrawEllipse(float[] field, Shape shape, double blend, double warpScale = 1, bool craggy = false)
    {
        double detail = craggy ? Math.Min(.024, Math.Min(shape.Rx, shape.Ry) * .32) * CaveSettings.Roughness : 0;
        double extent = Math.Max(shape.Rx, shape.Ry) * (1 + detail * 1.1 / Math.Min(shape.Rx, shape.Ry)) + Warp * 2 + blend;
        var point = new Point(0, 0);
        for (int y = Math.Max(0, (int)Math.Floor((shape.Y - extent) * Size)); y <= Math.Min(Size - 1, (int)Math.Ceiling((shape.Y + extent) * Size)); y++)
            for (int x = Math.Max(0, (int)Math.Floor((shape.X - extent) * Size)); x <= Math.Min(Size - 1, (int)Math.Ceiling((shape.X + extent) * Size)); x++)
            {
                int i = y * Size + x;
                point.X = Lerp((x + .5) / Size, Wx[i], warpScale); point.Y = Lerp((y + .5) / Size, Wy[i], warpScale);
                field[i] = (float)SmoothMin(field[i], Ellipse(point, shape) + WallDetail[i] * detail, blend);
            }
    }

    //扫掠左右不等宽截面，必要时一同绘制岸带。
    internal void Stroke(float[] field, List<Point> path, byte[] mask, double coreRadius, bool banks = false)
    {
        foreach (var p in path)
        {
            double left = p.Left + (banks ? p.BankLeft : 0), right = p.Right + (banks ? p.BankRight : 0);
            double half = (left + right) * .5, offset = (left - right) * .5;
            DrawEllipse(field, new Shape(p.X + p.Nx * offset, p.Y + p.Ny * offset, Math.Max(1.1 / Size, half * .55), half, p.Angle), 0, .3);
            if (mask != null) Disk(mask, p, coreRadius);
        }
    }

    //保护核心直接按离散格计算，避免浮点轮廓断裂。
    internal void Disk(byte[] mask, Point center, double radius)
    {
        double r = radius * Size, cx = center.X * Size - .5, cy = center.Y * Size - .5;
        for (int y = Math.Max(0, (int)Math.Floor(cy - r)); y <= Math.Min(Size - 1, (int)Math.Ceiling(cy + r)); y++)
            for (int x = Math.Max(0, (int)Math.Floor(cx - r)); x <= Math.Min(Size - 1, (int)Math.Ceiling(cx + r)); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) mask[y * Size + x] = 1;
    }

    //主轮廓叠加偏心叶瓣，避免规则椭圆。
    internal void Blob(float[] field, Shape shape, Func<double> random, int lobes)
    {
        bool craggy = field == Cave;
        DrawEllipse(field, shape, craggy ? .004 : .015, craggy: craggy);
        for (int i = 0; i < lobes; i++)
        {
            double a = random() * Math.PI * 2, reach = .55 + random() * .35;
            double c = Math.Cos(shape.Angle), s = Math.Sin(shape.Angle), x = Math.Cos(a) * shape.Rx * reach, y = Math.Sin(a) * shape.Ry * reach;
            DrawEllipse(field, new Shape(shape.X + x * c - y * s, shape.Y + x * s + y * c,
                shape.Rx * (.35 + random() * .2), shape.Ry * (.32 + random() * .2), shape.Angle + a),
                craggy ? .003 : .013, craggy: craggy);
        }
    }

    //截流水坑限制在终点洞室内，保留干燥岸带。
    internal void Pond(Shape pool)
    {
        var random = Random(pool.Seed); var knots = new double[13];
        for (int i = 0; i < knots.Length; i++) knots[i] = .58 + random() * .7;
        double extent = Math.Max(pool.Rx, pool.Ry) * 1.5, bank = 2.0 / Size, c = Math.Cos(pool.Angle), s = Math.Sin(pool.Angle);
        var point = new Point(0, 0);
        for (int y = Math.Max(0, (int)Math.Floor((pool.Y - extent) * Size)); y <= Math.Min(Size - 1, (int)Math.Ceiling((pool.Y + extent) * Size)); y++)
            for (int x = Math.Max(0, (int)Math.Floor((pool.X - extent) * Size)); x <= Math.Min(Size - 1, (int)Math.Ceiling((pool.X + extent) * Size)); x++)
            {
                int i = y * Size + x; point.X = Wx[i]; point.Y = Wy[i];
                double dx = point.X - pool.X, dy = point.Y - pool.Y, u = (dx * c + dy * s) / pool.Rx, v = (-dx * s + dy * c) / pool.Ry;
                double a = (Math.Atan2(v, u) + Math.PI) * knots.Length / (Math.PI * 2); int k = (int)Math.Floor(a);
                double radius = Lerp(knots[k % knots.Length], knots[(k + 1) % knots.Length], Smooth(a - k));
                double d = (Length(u, v) / radius - 1) * Math.Min(pool.Rx, pool.Ry);
                Water[i] = (float)Math.Min(Water[i], Math.Max(d, Math.Max(Ellipse(point, pool.Room) + bank, Cave[i] + bank)));
            }
    }

    //岩柱只占主洞内部，不切断保护通道、不覆盖水面。
    internal void AddPillars(CavePlan plan, CaveSettings settings)
    {
        var random = Random(settings.Seed + ":pillars"); var point = new Point(0, 0);
        for (int attempt = 0; plan.Pillars.Count < settings.Pillars && attempt < 300; attempt++)
        {
            var room = plan.Rooms[(int)(random() * plan.Rooms.Count)];
            var p = new Point(room.X + (random() - .5) * room.Rx * 1.5, room.Y + (random() - .5) * room.Ry * 1.5);
            double rx = .008 + random() * .017, ry = rx * (.65 + random() * .9);
            if (p.X <= .03 || p.Y <= .03 || p.X >= .97 || p.Y >= .97) continue;
            int i = (int)(p.Y * Size) * Size + (int)(p.X * Size);
            if (Cave[i] > -rx * 1.8 || Water[i] < rx * 1.7 || Distance(p, plan.Spawn) < .055) continue;
            if (plan.Pillars.Exists(e => Distance(e, p) < rx + e.Rx + .018)) continue;
            var shape = new Shape(p.X, p.Y, rx, ry, random() * Math.PI);
            double extent = Math.Max(rx, ry) + Warp * 2;
            for (int y = Math.Max(0, (int)Math.Floor((p.Y - extent) * Size)); y <= Math.Min(Size - 1, (int)Math.Ceiling((p.Y + extent) * Size)); y++)
                for (int x = Math.Max(0, (int)Math.Floor((p.X - extent) * Size)); x <= Math.Min(Size - 1, (int)Math.Ceiling((p.X + extent) * Size)); x++)
                {
                    int j = y * Size + x;
                    if (Protected[j] != 0) continue;
                    point.X = Wx[j]; point.Y = Wy[j];
                    Cave[j] = (float)Math.Max(Cave[j], -Ellipse(point, shape));
                }
            plan.Pillars.Add(shape);
        }
    }
}
