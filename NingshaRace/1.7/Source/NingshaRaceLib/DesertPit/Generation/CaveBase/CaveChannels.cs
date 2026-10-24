using System;
using System.Collections.Generic;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//构造自然曲线和独立变化的左右岸，不使用额外的小墙壁补丁。
internal static class CaveChannels
{
    //以实际弧长扰动走向，并在锚点归零以保持连接。
    internal static List<Point> Natural(IReadOnlyList<Point> anchors, string seed, bool river, CaveSettings settings)
    {
        var path = Spline(anchors, Math.Max(500, settings.Size * 2));
        var output = new List<Point>(path.Count);
        uint hash = Hash(seed + ":bends");
        double distance = 0;
        for (int i = 0; i < path.Count; i++)
        {
            var p = path[i];
            if (i > 0) distance += Distance(path[i - 1], p);
            var before = path[Math.Max(0, i - 1)]; var after = path[Math.Min(path.Count - 1, i + 1)];
            double angle = Math.Atan2(after.Y - before.Y, after.X - before.X);
            int segment = Math.Min(anchors.Count - 2, (int)Math.Floor(p.T * (anchors.Count - 1)));
            double t = p.T * (anchors.Count - 1) - segment;
            double envelope = Smooth(Math.Clamp(Math.Min(t, 1 - t) * 5, 0, 1));
            double amplitude = Math.Min(Distance(anchors[segment], anchors[segment + 1]) * .23, river ? .027 : .022) * settings.Meander;
            double bend = Noise(distance / .085, 7, hash) + .4 * Noise(distance / .031, 19, hash + 32);
            output.Add(new Point(p.X - Math.Sin(angle) * amplitude * bend * envelope,
                p.Y + Math.Cos(angle) * amplitude * bend * envelope) { T = p.T });
        }
        return output;
    }

    //按半格重新加密曲线，独立插值左右岸和岸带宽度。
    internal static List<Point> Profile(List<Point> path, string seed, bool river, CaveSettings settings, double scale = 1)
    {
        var dense = new List<Point> { new(path[0].X, path[0].Y) { T = path[0].T } };
        for (int i = 1; i < path.Count; i++)
        {
            var a = path[i - 1]; var b = path[i];
            int steps = (int)Math.Ceiling(Distance(a, b) * settings.Size * 2);
            for (int j = 1; j <= steps; j++)
            {
                double t = (double)j / steps;
                dense.Add(new Point(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t)) { T = Lerp(a.T, b.T, t) });
            }
        }
        var random = Random(seed + ":width");
        double variation = river ? CaveSettings.RiverVariation : CaveSettings.TunnelVariation;
        double basis = (river ? settings.WaterWidth : settings.TunnelWidth) * scale / (2 * settings.Size);
        double total = 0;
        for (int i = 0; i < dense.Count; i++)
        {
            if (i > 0) total += Distance(dense[i - 1], dense[i]);
            dense[i].S = total;
        }
        Point Station(double s)
        {
            double width = Math.Exp((random() - (river ? .46 : .58)) * (river ? 2.3 : 1.35) * variation);
            double skew = (random() - .5) * (river ? 1.05 : .8) * Math.Min(variation, 1.4);
            return new Point(0, 0) { S = s, Left = Math.Clamp(width * (1 + skew), .3, river ? 4 : 1.8),
                Right = Math.Clamp(width * (1 - skew), .3, river ? 4 : 1.8),
                BankLeft = .45 + Math.Pow(random(), .7) * 1.7, BankRight = .45 + Math.Pow(random(), .7) * 1.7 };
        }
        var stations = new List<Point> { Station(0) };
        for (double s = 0; s < total;)
        {
            s += river ? .018 + Math.Pow(random(), .65) * .105 : .012 + random() * .065;
            stations.Add(Station(s));
        }
        int index = 0;
        for (int i = 0; i < dense.Count; i++)
        {
            var p = dense[i];
            while (index < stations.Count - 2 && stations[index + 1].S < p.S) index++;
            var a = stations[index]; var b = stations[index + 1];
            double u = Smooth(Math.Clamp((p.S - a.S) / (b.S - a.S), 0, 1));
            var before = dense[Math.Max(0, i - 1)]; var after = dense[Math.Min(dense.Count - 1, i + 1)];
            p.Angle = Math.Atan2(after.Y - before.Y, after.X - before.X);
            p.Left = Math.Max(1.05 / settings.Size, basis * Lerp(a.Left, b.Left, u));
            p.Right = Math.Max(1.05 / settings.Size, basis * Lerp(a.Right, b.Right, u));
            p.BankLeft = Math.Max(1.5, settings.Banks * Lerp(a.BankLeft, b.BankLeft, u)) / settings.Size;
            p.BankRight = Math.Max(1.5, settings.Banks * Lerp(a.BankRight, b.BankRight, u)) / settings.Size;
        }
        return dense;
    }
}
