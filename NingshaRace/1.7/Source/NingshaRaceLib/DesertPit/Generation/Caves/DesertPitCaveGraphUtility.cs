using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

using NingshaRaceLib.DesertPit.Generation.Data;

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

            DesertPitCaveNode main = new DesertPitCaveNode
            {
                Center = mainCenter,
                RadiusX = data.MainRadiusX,
                RadiusZ = data.MainRadiusZ,
                Rotation = Rand.Range(0f, 180f),
                Main = true,
                Depth = 0,
                Parent = null
            };
            List<DesertPitCaveNode> nodes = new List<DesertPitCaveNode> { main };

            int branchCount = Rand.RangeInclusive(4, 6);
            float baseAngle = Rand.Range(0f, 360f);
            for (int i = 0; i < branchCount; i++)
            {
                GenerateBranch(map, data, nodes, main, baseAngle, branchCount, i);
            }

            return nodes;
        }

        //函数职责：从主洞室向外生成一条会轻微转向的主支洞及其侧洞。
        private static void GenerateBranch(Map map, DesertPitLayoutData data, List<DesertPitCaveNode> nodes, DesertPitCaveNode main, float baseAngle, int branchCount, int index)
        {
            float angle = baseAngle + index * 360f / branchCount + Rand.Range(-16f, 16f);
            Vector3 direction = Vector3Utility.FromAngleFlat(angle).normalized;
            DesertPitCaveNode parent = main;
            int roomCount = Rand.RangeInclusive(2, 4);

            for (int depth = 1; depth <= roomCount; depth++)
            {
                float radius = Mathf.Max(4.5f, Rand.Range(8f, 12f) - depth * Rand.Range(0.8f, 1.45f));
                float minimumDistance = parent.Main ? parent.MaxRadius * 0.95f : 16f;
                float maximumDistance = parent.Main ? parent.MaxRadius * 1.18f : 25f;
                DesertPitCaveNode node;
                Vector3 placedDirection;
                if (!TryCreateNode(map, nodes, parent, direction, minimumDistance, maximumDistance, radius, depth, out node, out placedDirection))
                {
                    break;
                }

                RegisterNode(nodes, data, node);
                direction = (direction * 0.68f + placedDirection * 0.32f).normalized;
                parent = node;

                if (Rand.Chance(0.35f))
                {
                    AddSideRoom(map, nodes, data, parent, direction, depth);
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

        //函数职责：在主分支旁尝试生成偏移小洞室，增加天然支洞和死路感。
        private static void AddSideRoom(Map map, List<DesertPitCaveNode> nodes, DesertPitLayoutData data, DesertPitCaveNode parent, Vector3 direction, int depth)
        {
            float sideSign = Rand.Chance(0.5f) ? 1f : -1f;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x) * sideSign;
            Vector3 preferredDirection = (side + direction * Rand.Range(-0.35f, 0.45f)).normalized;
            DesertPitCaveNode node;
            Vector3 placedDirection;
            if (TryCreateNode(map, nodes, parent, preferredDirection, 13f, 25f, Rand.Range(4f, 7.5f), depth + 1, out node, out placedDirection))
            {
                RegisterNode(nodes, data, node);
            }
        }

        //函数职责：让既有小洞室继续衍生一到两个短分支，形成自然的次级支洞。
        private static void AddChildBranches(Map map, List<DesertPitCaveNode> nodes, DesertPitLayoutData data, DesertPitCaveNode parent, Vector3 parentDirection, int depth)
        {
            int count = Rand.RangeInclusive(1, 2);
            for (int i = 0; i < count; i++)
            {
                float angleOffset = Rand.Range(45f, 105f) * (Rand.Chance(0.5f) ? 1f : -1f);
                Vector3 direction = Quaternion.AngleAxis(angleOffset, Vector3.up) * parentDirection;
                DesertPitCaveNode node;
                Vector3 placedDirection;
                if (TryCreateNode(map, nodes, parent, direction.normalized, 14f, 27f, Rand.Range(3.8f, 6.4f), depth + 1, out node, out placedDirection))
                {
                    RegisterNode(nodes, data, node);
                }
            }
        }

        //函数职责：多次尝试在首选方向附近寻找不贴边、不重叠的洞室位置。
        private static bool TryCreateNode(Map map, List<DesertPitCaveNode> nodes, DesertPitCaveNode parent, Vector3 preferredDirection, float minimumDistance, float maximumDistance, float radius, int depth, out DesertPitCaveNode node, out Vector3 placedDirection)
        {
            float aspect = Rand.Range(0.76f, 0.96f);
            float radiusX = radius;
            float radiusZ = radius * aspect;
            for (int attempt = 0; attempt < 18; attempt++)
            {
                float turnLimit = Mathf.Lerp(12f, 72f, attempt / 17f);
                float angleOffset = Rand.Range(-turnLimit, turnLimit);
                Vector3 direction = Quaternion.AngleAxis(angleOffset, Vector3.up) * preferredDirection;
                Vector3 side = new Vector3(-direction.z, 0f, direction.x);
                float distance = Rand.Range(minimumDistance, maximumDistance);
                Vector3 centerVector = parent.Center.ToVector3Shifted() + direction.normalized * distance + side * Rand.Range(-3.5f, 3.5f);
                IntVec3 center = centerVector.ToIntVec3();
                float maxRadius = Mathf.Max(radiusX, radiusZ);
                if (!FitsInterior(map, center, maxRadius) || OverlapsExisting(nodes, parent, center, maxRadius))
                {
                    continue;
                }

                placedDirection = (center.ToVector3Shifted() - parent.Center.ToVector3Shifted()).normalized;
                node = new DesertPitCaveNode
                {
                    Center = center,
                    RadiusX = radiusX,
                    RadiusZ = radiusZ,
                    Rotation = Mathf.Atan2(placedDirection.x, placedDirection.z) * Mathf.Rad2Deg + Rand.Range(-24f, 24f),
                    Main = false,
                    Depth = depth,
                    Parent = parent
                };
                return true;
            }

            node = null;
            placedDirection = preferredDirection;
            return false;
        }

        //函数职责：判断洞室连同自然边缘扰动是否完整落在地图内侧。
        private static bool FitsInterior(Map map, IntVec3 center, float radius)
        {
            int margin = Mathf.CeilToInt(radius) + 5;
            return center.x >= margin && center.z >= margin && center.x < map.Size.x - margin && center.z < map.Size.z - margin;
        }

        //函数职责：判断候选洞室是否会与非父级洞室过度重叠。
        private static bool OverlapsExisting(List<DesertPitCaveNode> nodes, DesertPitCaveNode parent, IntVec3 center, float radius)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                DesertPitCaveNode existing = nodes[i];
                if (existing == parent)
                {
                    continue;
                }

                float minimumSpacing = (existing.MaxRadius + radius) * 0.66f;
                if (center.DistanceTo(existing.Center) < minimumSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：把有效洞室加入拓扑与后续内容生成所用的小洞室列表。
        private static void RegisterNode(List<DesertPitCaveNode> nodes, DesertPitLayoutData data, DesertPitCaveNode node)
        {
            nodes.Add(node);
            data.SmallRooms.Add(node.Center);
        }

        //函数职责：按层级和距离排序取得除主洞室外的节点。
        public static List<DesertPitCaveNode> OrderedChildNodes(List<DesertPitCaveNode> nodes)
        {
            DesertPitCaveNode main = nodes[0];
            return nodes.Skip(1).OrderBy((DesertPitCaveNode node) => node.Depth).ThenBy((DesertPitCaveNode node) => node.Center.DistanceTo(main.Center)).ToList();
        }

        //函数职责：把盲洞终点限制在地图内侧，避免虫道钻出地图边界。
        public static IntVec3 InteriorCell(Map map, IntVec3 cell)
        {
            const int margin = 8;
            return new IntVec3(Mathf.Clamp(cell.x, margin, map.Size.x - margin - 1), 0, Mathf.Clamp(cell.z, margin, map.Size.z - margin - 1));
        }
    }
}
