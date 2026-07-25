using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Steps;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Caves
{
    //类职责：负责生成沙漠巨坑的主洞室、侧洞、小洞室和分支拓扑。
    internal static class DesertPitCaveGraphUtility
    {
        //函数职责：构建主洞室、侧洞、小洞室和死路洞的拓扑节点。
        public static List<DesertPitCaveNode> BuildCaveGraph(Map map, DesertPitLayoutData data)
        {
            IntVec3 mainCenter = map.Center + RandomCenterOffset();
            data.MainCenter = mainCenter;
            data.MainRadiusX = Rand.Range(28f, 36f);
            data.MainRadiusZ = Rand.Range(22f, 30f);

            List<DesertPitCaveNode> nodes = new List<DesertPitCaveNode>();
            nodes.Add(new DesertPitCaveNode
            {
                Center = mainCenter,
                Radius = Mathf.Max(data.MainRadiusX, data.MainRadiusZ),
                Main = true,
                Depth = 0
            });

            int branchCount = Rand.RangeInclusive(4, 6);
            float baseAngle = Rand.Range(0f, 360f);
            for (int i = 0; i < branchCount; i++)
            {
                GenerateBranch(map, data, nodes, baseAngle, branchCount, i);
            }

            return nodes;
        }

        //函数职责：为指定分支角度生成一条主支洞和若干侧洞。
        private static void GenerateBranch(Map map, DesertPitLayoutData data, List<DesertPitCaveNode> nodes, float baseAngle, int branchCount, int index)
        {
            float angle = baseAngle + index * 360f / branchCount + Rand.Range(-20f, 20f);
            Vector3 direction = Vector3Utility.FromAngleFlat(angle).normalized;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x);
            IntVec3 parent = data.MainCenter;
            int roomCount = Rand.RangeInclusive(2, 4);
            float distance = Rand.Range(34f, 44f);

            for (int depth = 1; depth <= roomCount; depth++)
            {
                Vector3 centerVector = data.MainCenter.ToVector3Shifted() + direction * distance + side * Rand.Range(-15f, 15f);
                IntVec3 center = ClampToInterior(map, centerVector.ToIntVec3(), 8);
                float radius = Mathf.Max(4.5f, Rand.Range(8f, 12f) - depth * Rand.Range(0.8f, 1.45f));
                nodes.Add(new DesertPitCaveNode
                {
                    Center = center,
                    Radius = radius,
                    Main = false,
                    Depth = depth
                });
                data.SmallRooms.Add(center);
                parent = center;
                distance += Rand.Range(18f, 28f);

                if (Rand.Chance(0.35f))
                {
                    AddSideRoom(map, nodes, data, parent, direction, side, depth);
                }

                if (Rand.Chance(0.6f))
                {
                    AddChildBranches(map, nodes, data, parent, direction, depth);
                }
            }
        }

        //函数职责：随机取得主洞室中心相对地图中心的偏移。
        private static IntVec3 RandomCenterOffset()
        {
            float angle = Rand.Range(0f, 360f);
            float distance = Rand.Range(4f, 9f);
            Vector3 offset = Vector3Utility.FromAngleFlat(angle) * distance;
            return new IntVec3(Mathf.RoundToInt(offset.x), 0, Mathf.RoundToInt(offset.z));
        }

        //函数职责：把洞室中心限制在地图内侧，避免洞体被边界裁断。
        private static IntVec3 ClampToInterior(Map map, IntVec3 cell, int margin)
        {
            return new IntVec3(Mathf.Clamp(cell.x, margin, map.Size.x - margin - 1), 0, Mathf.Clamp(cell.z, margin, map.Size.z - margin - 1));
        }

        //函数职责：在主分支旁生成偏移小洞室，增加天然支洞和死路感。
        private static void AddSideRoom(Map map, List<DesertPitCaveNode> nodes, DesertPitLayoutData data, IntVec3 parent, Vector3 direction, Vector3 side, int depth)
        {
            float sideSign = Rand.Chance(0.5f) ? 1f : -1f;
            Vector3 sideVector = parent.ToVector3Shifted() + side * sideSign * Rand.Range(13f, 28f) + direction * Rand.Range(-8f, 14f);
            IntVec3 center = ClampToInterior(map, sideVector.ToIntVec3(), 8);
            nodes.Add(new DesertPitCaveNode
            {
                Center = center,
                Radius = Rand.Range(4f, 7.5f),
                Main = false,
                Depth = depth + 1
            });
            data.SmallRooms.Add(center);
        }

        //函数职责：让既有小洞室继续衍生一到两个短分支，形成更自然的次级支洞。
        private static void AddChildBranches(Map map, List<DesertPitCaveNode> nodes, DesertPitLayoutData data, IntVec3 parent, Vector3 parentDirection, int depth)
        {
            int count = Rand.RangeInclusive(1, 2);
            for (int i = 0; i < count; i++)
            {
                float angleOffset = Rand.Range(45f, 105f) * (Rand.Chance(0.5f) ? 1f : -1f);
                Vector3 direction = Quaternion.AngleAxis(angleOffset, Vector3.up) * parentDirection;
                Vector3 endVector = parent.ToVector3Shifted() + direction.normalized * Rand.Range(14f, 28f);
                IntVec3 center = ClampToInterior(map, endVector.ToIntVec3(), 8);
                nodes.Add(new DesertPitCaveNode
                {
                    Center = center,
                    Radius = Rand.Range(3.8f, 6.4f),
                    Main = false,
                    Depth = depth + 1
                });
                data.SmallRooms.Add(center);
            }
        }

        //函数职责：为指定节点寻找更靠近主洞室且距离合理的父节点。
        public static DesertPitCaveNode FindParent(List<DesertPitCaveNode> nodes, DesertPitCaveNode node)
        {
            DesertPitCaveNode best = nodes[0];
            float bestScore = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++)
            {
                DesertPitCaveNode candidate = nodes[i];
                if (candidate == node || candidate.Depth >= node.Depth)
                {
                    continue;
                }

                float distance = candidate.Center.DistanceTo(node.Center);
                float score = distance + candidate.Depth * 8f;
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        //函数职责：按层级和距离排序取得除主洞室外的节点。
        public static List<DesertPitCaveNode> OrderedChildNodes(List<DesertPitCaveNode> nodes)
        {
            DesertPitCaveNode main = nodes[0];
            return nodes.Skip(1).OrderBy((DesertPitCaveNode node) => node.Depth).ThenBy((DesertPitCaveNode node) => node.Center.DistanceTo(main.Center)).ToList();
        }

        //函数职责：把任意点限制在地图内侧，供盲洞终点使用。
        public static IntVec3 InteriorCell(Map map, IntVec3 cell)
        {
            return ClampToInterior(map, cell, 8);
        }
    }
}
