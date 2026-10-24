using System;
using System.Collections.Generic;
using static NingshaRaceLib.DesertPit.Generation.CaveBase.CaveMath;
using Point = NingshaRaceLib.DesertPit.Generation.CaveBase.CavePlan.Point;

namespace NingshaRaceLib.DesertPit.Generation.CaveBase;

//将距离场落为岩体、干地、浅水、深水，并提供实际格网距离。
internal static class CaveRaster
{
    //采用四方向连接，与实验台通行检测一致。
    internal static IEnumerable<int> Neighbors(int i, int n)
    {
        int x = i % n, y = i / n;
        if (x > 0) yield return i - 1;
        if (x < n - 1) yield return i + 1;
        if (y > 0) yield return i - n;
        if (y < n - 1) yield return i + n;
    }

    //从岸边向内计算水深，保留洞道穿水处的浅水通路。
    internal static byte[] Classify(CaveFields f)
    {
        int n = f.Size; var cells = new byte[n * n];
        for (int i = 0; i < cells.Length; i++)
        {
            if (f.Cave[i] <= 0 || f.Protected[i] != 0) cells[i] = 1;
            if (f.Water[i] <= 0 || f.WaterCore[i] != 0) cells[i] = 2;
        }
        var depth = new int[cells.Length]; var queue = new int[cells.Length]; int head = 0, tail = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != 2) continue;
            int x = i % n, y = i / n;
            if (x == 0 || y == 0 || x == n - 1 || y == n - 1 || cells[i - 1] != 2 || cells[i + 1] != 2 || cells[i - n] != 2 || cells[i + n] != 2)
            { depth[i] = 1; queue[tail++] = i; }
        }
        while (head < tail)
        {
            int i = queue[head++];
            foreach (int j in Neighbors(i, n))
                if (cells[j] == 2 && depth[j] == 0) { depth[j] = depth[i] + 1; queue[tail++] = j; }
        }
        for (int i = 0; i < cells.Length; i++)
            if (depth[i] > Math.Max(2, n / 100.0) && f.Protected[i] == 0) cells[i] = 3;
        return cells;
    }

    //入口靠近模板锚点，并具有干地净空。
    internal static int ChooseStart(CaveFields fields, byte[] cells, Point preferred)
    {
        int start = -1; double best = double.PositiveInfinity; var p = new Point(0, 0);
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != 1 || fields.Cave[i] > -.018 || fields.Water[i] < .025) continue;
            p.X = (i % fields.Size + .5) / fields.Size; p.Y = (i / fields.Size + .5) / fields.Size;
            double distance = Distance(p, preferred);
            if (distance < best) { best = distance; start = i; }
        }
        if (start < 0) throw new InvalidOperationException("洞穴缺少足够的入口干地。");
        return start;
    }

    //干地与浅水可通行，深水和岩体不可通行。
    internal static (int[] Distance, int[] Parent) Walking(byte[] cells, int n, int start)
    {
        var distance = new int[cells.Length]; var parent = new int[cells.Length]; var queue = new int[cells.Length];
        Array.Fill(distance, -1); Array.Fill(parent, -1);
        int head = 0, tail = 1; queue[0] = start; distance[start] = 0;
        while (head < tail)
        {
            int i = queue[head++];
            foreach (int j in Neighbors(i, n))
            {
                if (distance[j] >= 0 || (cells[j] != 1 && cells[j] != 2)) continue;
                distance[j] = distance[i] + 1; parent[j] = i; queue[tail++] = j;
            }
        }
        return (distance, parent);
    }
}
