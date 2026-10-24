using System;
using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation.Chambers
{
    //类职责：在实体生成前检查自然洞室的出口截面、岩壁、场景占地与洞外视线遮挡。
    internal static class AntChamberValidator
    {
        //函数职责：确认洞室只有一组相邻出口，岩层未被重连路线打穿，巢穴和菌巢可以完整落地。
        public static void Validate(Map map, AntChamberLayout room)
        {
            if (!room.OpenCells.Contains(room.Mouth) || room.GatewayCells.Count < 3)
                throw new InvalidOperationException("蚁巢自然洞口缺失或通行宽度不足三格。");
            HashSet<IntVec3> gateways = new HashSet<IntVec3> { room.Mouth };
            Queue<IntVec3> queue = new Queue<IntVec3>();
            queue.Enqueue(room.Mouth);
            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (room.GatewayCells.Contains(next) && gateways.Add(next)) queue.Enqueue(next);
                }
            }
            if (gateways.Count != room.GatewayCells.Count)
                throw new InvalidOperationException("蚁巢自然洞口形成了多个分离的出口。");
            foreach (IntVec3 cell in room.Footprint)
            {
                bool open = room.OpenCells.Contains(cell);
                if ((MapGenerator.Caves[cell] > 0f) != open)
                    throw new InvalidOperationException("蚁巢自然岩层或室内地面被其他连接路线改写。");
                if (!open) continue;
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                    if (!room.Footprint.Contains(cell + offset) && !room.GatewayCells.Contains(cell))
                        throw new InvalidOperationException("蚁巢自然洞室侧壁出现了非预期入口。");
            }
            RequireFloor(room, GenAdj.OccupiedRect(room.Nest, Rot4.North, DefOfRefs.NingshaRace_DesertPitAntNest.size).ExpandedBy(2));
            RequireFloor(room, GenAdj.OccupiedRect(room.Mound, Rot4.North, DefOfRefs.NingshaRace_FungalMound.size));
            CheckScreenedEntrance(room);
        }

        //函数职责：核对建筑占地和巢穴储藏环均在室内开放地面中，不靠后续清理岩块强行落地。
        private static void RequireFloor(AntChamberLayout room, CellRect occupied)
        {
            foreach (IntVec3 cell in occupied)
                if (!room.OpenCells.Contains(cell) || room.PassageCells.Contains(cell))
                    throw new InvalidOperationException("自然蚁巢洞室没有容纳完整的建筑占地或储藏区。");
        }

        //函数职责：从洞口外的多排横向格检查到蚁穴各占地格的射线，确认弯道岩脊仍然遮挡洞外射击。
        private static void CheckScreenedEntrance(AntChamberLayout room)
        {
            IntVec3 forward = AntChamberLayout.Rotate(new IntVec3(1, 0, 0), room.Direction);
            IntVec3 side = AntChamberLayout.Rotate(new IntVec3(0, 0, 1), room.Direction);
            CellRect nest = GenAdj.OccupiedRect(room.Nest, Rot4.North, DefOfRefs.NingshaRace_DesertPitAntNest.size);
            for (int depth = 0; depth <= 8; depth += 2)
                for (int lateral = -8; lateral <= 8; lateral++)
                {
                    IntVec3 source = room.Mouth + forward * depth + side * lateral;
                    foreach (IntVec3 target in nest)
                    {
                        bool blocked = false;
                        foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(source, target))
                            if (room.Footprint.Contains(cell) && !room.OpenCells.Contains(cell)) { blocked = true; break; }
                        if (!blocked) throw new InvalidOperationException("自然蚁巢入口的岩脊无法遮挡洞外对蚁穴的射击。");
                    }
                }
        }
    }
}
