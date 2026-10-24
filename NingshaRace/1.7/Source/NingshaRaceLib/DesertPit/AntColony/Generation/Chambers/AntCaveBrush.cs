using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：用圆形笔刷和连续曲线构造洞穴格集合，不读写地图或生成实体。
    internal static class AntCaveBrush
    {
        //函数职责：按格心到浮点圆心的距离填充圆盘，避免方形笔刷留下直角轮廓。
        public static void AddDisc(HashSet<IntVec3> cells, Vector2 center, float radius)
        {
            for (int x = Mathf.FloorToInt(center.x - radius); x <= Mathf.CeilToInt(center.x + radius); x++)
                for (int z = Mathf.FloorToInt(center.y - radius); z <= Mathf.CeilToInt(center.y + radius); z++)
                    if ((new Vector2(x, z) - center).sqrMagnitude <= radius * radius)
                        cells.Add(new IntVec3(x, 0, z));
        }

        //函数职责：以密集采样的三次曲线和渐变半径形成连续弯道，宽度变化不消耗逐格随机数。
        public static void AddCurve(HashSet<IntVec3> cells, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float phase)
        {
            int steps = Mathf.CeilToInt((Vector2.Distance(a, b) + Vector2.Distance(b, c) + Vector2.Distance(c, d)) * 4f);
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float u = 1f - t;
                Vector2 center = u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
                float radius = 1.85f + 0.3f * Mathf.Sin(t * 4.2f + phase);
                AddDisc(cells, center, radius);
            }
        }
    }
}
