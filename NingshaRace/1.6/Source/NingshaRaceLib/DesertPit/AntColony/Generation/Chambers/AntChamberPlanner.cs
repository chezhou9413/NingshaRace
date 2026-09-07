using System;
using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Generation.Data;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：在实体地形生成之前规划两个独立单口洞室，保留既有主通道的连通关系。
    public static class AntChamberPlanner
    {
        //函数职责：仅为自然巨坑和凝砂开局生成洞室，任务地图保持原有开放地形。
        public static void Plan(Map map, DesertPitLayoutData data)
        {
            data.AntChambers.Clear();
            if (map.generatorDef != DefOfRefs.NingshaRace_DesertPitMap && map.generatorDef != DefOfRefs.NingshaRace_DesertPitStartingMap) return;
            for (int index = 0; index < 2; index++)
            {
                AntChamberLayout room = FindRoom(map, data);
                if (room == null) throw new InvalidOperationException("沙漠巨坑没有足够空间容纳两个单口蚁巢洞室，请检查地图尺寸和布局参数。");
                foreach (IntVec3 cell in room.Footprint)
                {
                    MapGenerator.Caves[cell] = room.OpenCells.Contains(cell) ? 2f : 0f;
                    data.ReservedSceneCells.Add(cell);
                    data.ProtectedRouteCells.Remove(cell);
                }
                data.ProtectedRouteCells.UnionWith(room.PassageCells);
                HashSet<IntVec3> connected = AntChamberConnector.CollectMainCave(map, data.MainCenter);
                AntChamberConnector.Connect(map, data, room, connected);
                data.AntChambers.Add(room);
            }
            AntChamberOuterRoutes.Reconnect(map, data);
            foreach (AntChamberLayout room in data.AntChambers) AntChamberValidator.Validate(map, room);
        }

        //函数职责：先用包围盒快速排除占用，再以岩层行区间精确核对，避免空白角落影响小地图选址。
        private static AntChamberLayout FindRoom(Map map, DesertPitLayoutData data)
        {
            int[,] blocked = BuildPrefix(map, data, false);
            int[,] routes = BuildPrefix(map, data, true);
            AntChamberShape shape = new AntChamberShape();
            List<CellRect>[] spans = RotateSpans(shape);
            IntVec3 bestCenter = IntVec3.Invalid;
            int bestDirection = 0;
            float bestScore = float.MinValue;
            foreach (IntVec3 center in map.AllCells)
            {
                if (center.x % 2 != 0 || center.z % 2 != 0) continue;
                for (int direction = 0; direction < 4; direction++)
                {
                    CellRect rect = AntChamberLayout.BoundsAt(shape.Bounds, center, direction);
                    IntVec3 mouth = center + AntChamberLayout.Rotate(shape.Mouth, direction);
                    if (rect.minX < 3 || rect.minZ < 3 || rect.maxX >= map.Size.x - 3 || rect.maxZ >= map.Size.z - 3) continue;
                    if (mouth.DistanceToSquared(data.MainCenter) >= center.DistanceToSquared(data.MainCenter)) continue;
                    if (CountRect(blocked, rect) != 0 && CountSpans(blocked, spans[direction], center, true) != 0) continue;
                    int crossedRoutes = CountRect(routes, rect) == 0 ? 0 : CountSpans(routes, spans[direction], center, false);
                    float score = center.DistanceTo(data.MainCenter) - mouth.DistanceTo(data.MainCenter) * 0.3f - crossedRoutes * 10f;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    bestCenter = center;
                    bestDirection = direction;
                }
            }
            return bestCenter.IsValid ? new AntChamberLayout(bestCenter, bestDirection, shape) : null;
        }

        //函数职责：为四个朝向各旋转一次局部占地区间，候选循环只平移区间，不构造新的洞室对象。
        private static List<CellRect>[] RotateSpans(AntChamberShape shape)
        {
            List<CellRect>[] result = new List<CellRect>[4];
            for (int direction = 0; direction < 4; direction++)
            {
                result[direction] = new List<CellRect>();
                foreach (CellRect span in shape.OccupiedSpans)
                    result[direction].Add(AntChamberLayout.BoundsAt(span, new IntVec3(0, 0, 0), direction));
            }
            return result;
        }

        //函数职责：通过互不重叠的行区间累计实际占用，禁用区检查遇到占用立即结束。
        private static int CountSpans(int[,] prefix, List<CellRect> spans, IntVec3 origin, bool stopOnOccupied)
        {
            int count = 0;
            foreach (CellRect span in spans)
            {
                CellRect translated = CellRect.FromLimits(
                    new IntVec3(span.minX + origin.x, 0, span.minZ + origin.z),
                    new IntVec3(span.maxX + origin.x, 0, span.maxZ + origin.z));
                count += CountRect(prefix, translated);
                if (stopOnOccupied && count != 0) break;
            }
            return count;
        }

        //函数职责：常数时间查询矩形中的占用数量，供快速筛选和实际岩层区间复用。
        private static int CountRect(int[,] prefix, CellRect rect)
        {
            return prefix[rect.maxX + 1, rect.maxZ + 1] - prefix[rect.minX, rect.maxZ + 1]
                - prefix[rect.maxX + 1, rect.minZ] + prefix[rect.minX, rect.minZ];
        }

        //函数职责：分别缓存不可覆盖区域和既有通道数量，优先避开通道并让被截断的支洞在室外重新连接。
        private static int[,] BuildPrefix(Map map, DesertPitLayoutData data, bool routesOnly)
        {
            int[,] prefix = new int[map.Size.x + 1, map.Size.z + 1];
            for (int x = 0; x < map.Size.x; x++)
                for (int z = 0; z < map.Size.z; z++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    bool blocked = routesOnly ? data.ProtectedRouteCells.Contains(cell)
                        : data.ReservedSceneCells.Contains(cell) || cell.DistanceToSquared(data.MainCenter) < 35 * 35;
                    int occupied = blocked ? 1 : 0;
                    prefix[x + 1, z + 1] = occupied + prefix[x, z + 1] + prefix[x + 1, z] - prefix[x, z];
                }
            return prefix;
        }
    }
}
