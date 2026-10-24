using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Regions;

//在生成步骤间保留分区与净空；运行时只保存最终生态类型。
internal sealed class CaveEcologyLayout
{
    internal CavePlan Plan;
    internal CaveSettings Settings;
    internal byte[] Cells, Oasis, Light, Reserved;
    internal int[] Walking, WallDistance;
    internal int Start;
    internal int Size => Settings.Size;
    internal IntVec3 Cell(int index) => new(index % Size, 0, index / Size);

    //八方向距离用于岸边湿度、洞壁净空及独立灰暗区的间隔。
    internal static (int[] Distance, int[] Source) Proximity(int n, Func<int, bool> source)
    {
        int count = n * n;
        var distance = new int[count]; var origin = new int[count]; var queue = new int[count];
        Array.Fill(distance, n * 2); Array.Fill(origin, -1);
        int head = 0, tail = 0;
        for (int i = 0; i < count; i++)
            if (source(i)) { distance[i] = 0; origin[i] = i; queue[tail++] = i; }
        while (head < tail)
        {
            int i = queue[head++], x = i % n, y = i / n;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int xx = x + dx, yy = y + dy, j = yy * n + xx;
                    if (xx < 0 || yy < 0 || xx >= n || yy >= n || distance[j] <= distance[i] + 1) continue;
                    distance[j] = distance[i] + 1; origin[j] = origin[i]; queue[tail++] = j;
                }
        }
        return (distance, origin);
    }

    //把预览中的绿洲和明暗叠层归并为植被群落，水面、巢室与不可达地面不参与散布。
    internal byte RegionAt(int i)
    {
        if (Cells[i] != 1 || Walking[i] < 0 || Reserved[i] != 0) return 0;
        if (Oasis[i] != 0)
            return Light[i] is 1 or 2 ? (byte)(Oasis[i] switch { 2 => 6, 3 => 7, _ => 8 }) : (byte)9;
        return (byte)(Light[i] switch { 2 => 4, 1 => 5, 3 => 2, 4 => 3, _ => 1 });
    }

    //地形完成后计算分区，随机流与实验台的默认 v14 设置一致。
    internal static CaveEcologyLayout Build(byte[] cells, int start, CavePlan plan, CaveSettings settings)
    {
        var layout = new CaveEcologyLayout { Cells = cells, Start = start, Plan = plan, Settings = settings,
            Reserved = new byte[cells.Length], Walking = CaveRaster.Walking(cells, settings.Size, start).Distance };
        foreach (var room in plan.EndRooms)
            foreach (int i in room.Floor) layout.Reserved[i] = 1;
        layout.WallDistance = Proximity(settings.Size, i => cells[i] == 0).Distance;
        layout.Oasis = CaveOasisRegions.Build(layout);
        layout.Light = CaveLightRegions.Build(layout);
        return layout;
    }
}
