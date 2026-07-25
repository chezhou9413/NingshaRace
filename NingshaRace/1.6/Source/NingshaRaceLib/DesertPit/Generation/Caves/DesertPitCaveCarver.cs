using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Steps;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Caves
{
    //类职责：负责把沙漠巨坑拓扑节点雕刻成融合洞室、虫道隧道和连通洞穴掩码。
    internal static class DesertPitCaveCarver
    {
        //函数职责：用多个圆团融合生成不规则洞室，替代规则椭圆。
        public static void CarveRooms(Map map, List<DesertPitCaveNode> nodes)
        {
            ModuleBase warpNoise = new Perlin(0.05000000074505806, 2.0, 0.5, 5, Rand.Int, QualityMode.Medium);
            for (int i = 0; i < nodes.Count; i++)
            {
                DesertPitCaveNode node = nodes[i];
                int blobCount = node.Main ? Rand.RangeInclusive(12, 17) : Rand.RangeInclusive(4, 6);
                float maxOffset = node.Main ? node.Radius * 0.62f : node.Radius * 0.34f;
                for (int blob = 0; blob < blobCount; blob++)
                {
                    Vector3 offset = Vector3Utility.FromAngleFlat(Rand.Range(0f, 360f)) * Rand.Range(0f, maxOffset);
                    Vector3 center = node.Center.ToVector3Shifted() + offset;
                    float radius = node.Radius * Rand.Range(node.Main ? 0.38f : 0.38f, node.Main ? 0.68f : 0.74f);
                    CarveWarpedBlob(map, center, radius, warpNoise, node.Main ? 1f : 0.9f, node.Main);
                }
            }
        }

        //函数职责：按拓扑节点生成主分支、侧支和少量回环虫道。
        public static void CarveGraphTunnels(Map map, List<DesertPitCaveNode> nodes)
        {
            List<DesertPitCaveNode> ordered = DesertPitCaveGraphUtility.OrderedChildNodes(nodes);
            for (int i = 0; i < ordered.Count; i++)
            {
                DesertPitCaveNode node = ordered[i];
                DesertPitCaveNode parent = DesertPitCaveGraphUtility.FindParent(nodes, node);
                CarvePerlinWorm(map, parent.Center, node.Center, Rand.Range(1.45f, 3.15f), node.Depth);
            }

            int loopCount = Rand.RangeInclusive(2, 4);
            for (int i = 0; i < loopCount; i++)
            {
                DesertPitCaveNode first = ordered.RandomElement();
                DesertPitCaveNode second = ordered.RandomElement();
                if (first != second && first.Center.DistanceTo(second.Center) < 68f)
                {
                    CarvePerlinWorm(map, first.Center, second.Center, Rand.Range(1.1f, 2.05f), Mathf.Max(first.Depth, second.Depth));
                }
            }
        }

        //函数职责：生成少量半堵死洞和盲肠支路，增加探索层次。
        public static void CarveBlindPockets(Map map, DesertPitLayoutData data, List<DesertPitCaveNode> nodes)
        {
            int count = Rand.RangeInclusive(4, 7);
            for (int i = 0; i < count; i++)
            {
                DesertPitCaveNode origin = nodes.RandomElement();
                Vector3 direction = Vector3Utility.FromAngleFlat(Rand.Range(0f, 360f));
                IntVec3 end = DesertPitCaveGraphUtility.InteriorCell(map, (origin.Center.ToVector3Shifted() + direction * Rand.Range(16f, 38f)).ToIntVec3());
                CarvePerlinWorm(map, origin.Center, end, Rand.Range(0.9f, 1.75f), origin.Depth + 1);
                if (Rand.Chance(0.55f))
                {
                    data.Collapses.Add(end);
                }
            }
        }

        //函数职责：对洞穴边缘执行少量细胞自动机侵蚀，让边界不再是一刀切。
        public static void RunEdgeErosion(Map map, List<DesertPitCaveNode> nodes)
        {
            HashSet<IntVec3> toOpen = new HashSet<IntVec3>();
            HashSet<IntVec3> toClose = new HashSet<IntVec3>();
            for (int pass = 0; pass < 2; pass++)
            {
                CollectEdgeChanges(map, nodes, toOpen, toClose);
                foreach (IntVec3 cell in toClose)
                {
                    MapGenerator.Caves[cell] = 0f;
                }

                foreach (IntVec3 cell in toOpen)
                {
                    DesertPitGenUtility.MarkCave(cell, map, 0.45f);
                }
            }
        }

        //函数职责：确保主洞室和所有记录洞室在侵蚀后仍然连通。
        public static void EnsureGraphConnected(Map map, List<DesertPitCaveNode> nodes)
        {
            HashSet<IntVec3> reachable = FloodFrom(map, nodes[0].Center);
            for (int i = 1; i < nodes.Count; i++)
            {
                if (!reachable.Contains(nodes[i].Center))
                {
                    CarvePerlinWorm(map, nodes[0].Center, nodes[i].Center, Rand.Range(1.6f, 2.6f), nodes[i].Depth);
                    reachable = FloodFrom(map, nodes[0].Center);
                }
            }
        }

        //函数职责：挖出带噪声扭曲边界的圆团洞体。
        private static void CarveWarpedBlob(Map map, Vector3 center, float radius, ModuleBase warpNoise, float strength, bool roughEdge)
        {
            IntVec3 root = center.ToIntVec3();
            int maxRadius = Mathf.CeilToInt(radius + 4f);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(root, maxRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                float noise = (float)warpNoise.GetValue(cell.x, cell.z * 0.73f, root.x * 0.19f);
                float chipNoise = (float)warpNoise.GetValue(cell.x * 1.9f, root.z * 0.31f, cell.z * 1.9f);
                float warpedRadius = radius + noise * (roughEdge ? 5.2f : 2.6f) + chipNoise * (roughEdge ? 1.8f : 0.4f);
                float distance = Vector3.Distance(cell.ToVector3Shifted(), center);
                bool chippedWall = roughEdge && distance > warpedRadius - 2.4f && chipNoise < -0.42f && Rand.Chance(0.38f);
                if (distance <= warpedRadius && !chippedWall)
                {
                    float localStrength = Mathf.Clamp01(1f - distance / Mathf.Max(warpedRadius, 1f));
                    DesertPitGenUtility.MarkCave(cell, map, Mathf.Max(localStrength * strength, 0.48f));
                }
            }
        }

        //函数职责：使用带惯性和噪声扰动的虫道连接两个洞室。
        private static void CarvePerlinWorm(Map map, IntVec3 start, IntVec3 target, float baseWidth, int depth)
        {
            Vector3 position = start.ToVector3Shifted();
            Vector3 targetVector = target.ToVector3Shifted();
            Vector3 heading = (targetVector - position).normalized;
            ModuleBase turnNoise = new Perlin(0.052000001072883606, 2.0, 0.5, 5, Rand.Int, QualityMode.Medium);
            ModuleBase widthNoise = new Perlin(0.12000000476837158, 2.0, 0.5, 3, Rand.Int, QualityMode.Medium);
            int guard = 0;

            while (Vector3.Distance(position, targetVector) > 2.1f && guard < 420)
            {
                Vector3 toTarget = (targetVector - position).normalized;
                Vector3 side = new Vector3(-heading.z, 0f, heading.x);
                float turn = (float)turnNoise.GetValue(position.x, guard * 0.47f, position.z);
                heading = (heading * 0.74f + toTarget * 0.34f + side * turn * 1.18f).normalized;
                float widthWave = (float)widthNoise.GetValue(position.x, 0.0, position.z);
                float taper = Mathf.Clamp01((float)guard / 260f) * 0.32f;
                float width = Mathf.Max(0.9f, baseWidth + widthWave * 0.72f - taper - depth * 0.11f);
                if (Rand.Chance(0.025f))
                {
                    width *= Rand.Range(1.15f, 1.55f);
                }

                DesertPitGenUtility.CarveCircle(map, position, width, 0.85f);
                position += heading * Rand.Range(0.62f, 0.96f);
                guard++;
            }

            DesertPitGenUtility.CarveCircle(map, targetVector, Mathf.Max(baseWidth, 1.85f), 0.9f);
        }

        //函数职责：收集当前细胞自动机迭代需要打开和关闭的边缘格。
        private static void CollectEdgeChanges(Map map, List<DesertPitCaveNode> nodes, HashSet<IntVec3> toOpen, HashSet<IntVec3> toClose)
        {
            toOpen.Clear();
            toClose.Clear();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.DistanceToEdge(map) < 4)
                {
                    continue;
                }

                int caveNeighbors = CountCaveNeighbors(map, cell, 1);
                bool cave = DesertPitGenUtility.IsCave(map, cell);
                if (cave && caveNeighbors <= 2 && !NearImportantNode(nodes, cell, 7f))
                {
                    toClose.Add(cell);
                }
                else if (!cave && caveNeighbors >= 6 && Rand.Chance(0.72f))
                {
                    toOpen.Add(cell);
                }
            }
        }

        //函数职责：统计周围八邻域洞穴数量。
        private static int CountCaveNeighbors(Map map, IntVec3 cell, int radius)
        {
            int count = 0;
            foreach (IntVec3 check in GenRadial.RadialCellsAround(cell, radius, useCenter: false))
            {
                if (DesertPitGenUtility.IsCave(map, check))
                {
                    count++;
                }
            }

            return count;
        }

        //函数职责：判断格子是否靠近必须保留的洞室中心。
        private static bool NearImportantNode(List<DesertPitCaveNode> nodes, IntVec3 cell, float radius)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (cell.DistanceTo(nodes[i].Center) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：从指定洞穴格开始泛洪取得连通洞穴区域。
        private static HashSet<IntVec3> FloodFrom(Map map, IntVec3 root)
        {
            HashSet<IntVec3> result = new HashSet<IntVec3>();
            if (!DesertPitGenUtility.IsCave(map, root))
            {
                DesertPitGenUtility.MarkCave(root, map, 1f);
            }

            map.floodFiller.FloodFill(root, (IntVec3 cell) => DesertPitGenUtility.IsCave(map, cell), delegate(IntVec3 cell)
            {
                result.Add(cell);
            });
            return result;
        }
    }
}
