using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;
using Shape = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Shape;

namespace NingshaRaceLib.DesertPit.Generation.Regions;

//从暗河、水潭与大洞室中挑选独立灰暗来源，避免生成散乱圆斑。
internal static class CaveDimRegions
{
    //候选保留原始水系或洞室形状，选中后才栅格化。
    private sealed class Candidate
    {
        internal Point Center;
        internal List<Point> River;
        internal Shape Pool, Room;
        internal double Begin, End, Score;
    }

    //挑出远离亮区且彼此分隔的三处连续区域。
    internal static byte[] Sources(CaveEcologyLayout layout, byte[] zones)
    {
        int n = layout.Size; var cells = layout.Cells;
        var distance = CaveEcologyLayout.Proximity(n, i => zones[i] is 1 or 2).Distance;
        double gap = n * .1; var random = Random(layout.Settings.Seed + ":independent-dim");
        var candidates = new List<Candidate>();
        foreach (var river in layout.Plan.Rivers)
        {
            var run = new List<Point>();
            foreach (var p in river.Concat(new Point[] { null }))
            {
                int i = p == null ? -1 : (int)Math.Floor(p.Y * n) * n + (int)Math.Floor(p.X * n);
                if (p != null && p.X >= 0 && p.Y >= 0 && p.X < 1 && p.Y < 1 && cells[i] >= 2 && distance[i] >= gap) run.Add(p);
                else if (run.Count > 0)
                {
                    double length = run[^1].S - run[0].S, span = Math.Min(.36, length);
                    if (length >= .14)
                    {
                        double begin = run[0].S + random() * (length - span), end = begin + span;
                        candidates.Add(new Candidate { River = river, Begin = begin, End = end,
                            Center = run.First(q => q.S >= (begin + end) * .5), Score = 2 + span + random() * .15 });
                    }
                    run.Clear();
                }
            }
        }
        foreach (var pool in layout.Plan.Pools)
            if (distance[(int)(pool.Y * n) * n + (int)(pool.X * n)] >= gap)
                candidates.Add(new Candidate { Pool = pool, Center = pool, Score = 2.1 + random() * .3 });
        foreach (var room in layout.Plan.Rooms)
            if (!room.Nest && room.Rx * room.Ry >= .002 && distance[(int)(room.Y * n) * n + (int)(room.X * n)] >= gap)
                candidates.Add(new Candidate { Room = room, Center = room, Score = 1 + Math.Min(room.Rx * room.Ry * 40, .8) + random() * .2 });
        var sources = new byte[cells.Length]; var selected = new List<Point>();
        foreach (var candidate in candidates.OrderByDescending(c => c.Score))
        {
            if (selected.Count >= 3) break;
            if (selected.Any(p => Distance(p, candidate.Center) < .2)) continue;
            var mask = new byte[cells.Length];
            if (candidate.River != null) CaveLightRegions.RiverSources(mask, candidate.River, candidate.End, layout, candidate.Begin);
            else if (candidate.Pool != null) CaveLightRegions.PoolSources(mask, candidate.Pool, layout);
            else RoomSources(mask, candidate.Room, layout);
            var indices = new List<int>(); bool dryContact = false;
            for (int i = 0; i < cells.Length; i++)
            {
                if (mask[i] == 0 || layout.Reserved[i] != 0 || distance[i] < gap || zones[i] != 0) continue;
                indices.Add(i);
                if (cells[i] == 1 && layout.Walking[i] >= 0) dryContact = true;
                foreach (int j in CaveRaster.Neighbors(i, n))
                    if (cells[j] == 1 && layout.Walking[j] >= 0 && layout.Reserved[j] == 0 && zones[j] == 0) dryContact = true;
            }
            if (!dryContact || indices.Count < n * .05) continue;
            foreach (int i in indices) sources[i] = 1;
            selected.Add(candidate.Center);
        }
        return sources;
    }

    //大洞室只取腹地作为灰暗源，再由公共扩散步骤自然向洞口延伸。
    private static void RoomSources(byte[] mask, Shape room, CaveEcologyLayout layout)
    {
        int n = layout.Size; double extent = Math.Max(room.Rx, room.Ry);
        for (int y = Math.Max(0, (int)Math.Floor((room.Y - extent) * n)); y <= Math.Min(n - 1, (int)Math.Ceiling((room.Y + extent) * n)); y++)
            for (int x = Math.Max(0, (int)Math.Floor((room.X - extent) * n)); x <= Math.Min(n - 1, (int)Math.Ceiling((room.X + extent) * n)); x++)
            {
                int i = y * n + x;
                if (layout.Cells[i] == 1 && layout.Walking[i] >= 0 && Ellipse(new Point((x + .5) / n, (y + .5) / n), room) < -Math.Min(room.Rx, room.Ry) * .4)
                    mask[i] = 1;
            }
    }
}
