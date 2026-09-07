using System;
using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Data;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：把已完成寻路的中心线塑成圆润变宽的洞道，最后在生成线程统一提交洞穴掩码。
    internal static class AntTunnelCarver
    {
        //函数职责：圆滑处理中心线折点并叠加缓变宽度，保留正交连通核心且不穿透保护岩壁。
        public static void Carve(Map map, DesertPitLayoutData data, List<IntVec3> path, Func<IntVec3, bool> canOpen)
        {
            HashSet<IntVec3> floor = new HashSet<IntVec3>();
            ModuleBase noise = new Perlin(0.12, 2.0, 0.5, 2, Rand.Int, QualityMode.Medium);
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 current = new Vector2(path[i].x, path[i].z);
                Vector2 before = i == 0 ? current : new Vector2(path[i - 1].x, path[i - 1].z);
                Vector2 after = i == path.Count - 1 ? current : new Vector2(path[i + 1].x, path[i + 1].z);
                Vector2 a = (before + current) * 0.5f;
                Vector2 b = (current + after) * 0.5f;
                for (int sample = 0; sample <= 6; sample++)
                {
                    float t = sample / 6f;
                    Vector2 point = (1f - t) * (1f - t) * a + 2f * (1f - t) * t * current + t * t * b;
                    float radius = 2.05f + 0.35f * Mathf.Clamp((float)noise.GetValue(point.x, 0, point.y), -1f, 1f);
                    AntCaveBrush.AddDisc(floor, point, radius);
                }
                if (i > 0)
                    foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(path[i - 1], path[i])) floor.Add(cell);
            }

            //取与路线实际连通的笔刷区域，防止裁去保护格后在岩壁另一侧留下孤立小孔。
            floor.RemoveWhere(cell => !cell.InBounds(map) || !canOpen(cell));
            HashSet<IntVec3> connected = new HashSet<IntVec3>();
            Queue<IntVec3> queue = new Queue<IntVec3>();
            if (floor.Contains(path[0])) { connected.Add(path[0]); queue.Enqueue(path[0]); }
            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (floor.Contains(next) && connected.Add(next)) queue.Enqueue(next);
                }
            }
            if (!connected.Contains(path[path.Count - 1]))
                throw new InvalidOperationException("自然洞道雕刻未能保持中心线连通，未写入地图。");
            foreach (IntVec3 cell in connected)
            {
                MapGenerator.Caves[cell] = 2f;
                data.ReservedSceneCells.Add(cell);
                data.ProtectedRouteCells.Add(cell);
            }
        }
    }
}
