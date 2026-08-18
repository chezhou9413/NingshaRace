using System;
using System.Collections.Generic;
using NingshaRaceLib.GiantTomb.Layout;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Generation
{
    //类职责：收集单个模板内远离连接口的空地，并统一预留敌人、奖励和蚁群占用格。
    internal sealed class GiantTombContentCellPool
    {
        private readonly Map map;
        private readonly GiantTombPlacement placement;
        private readonly List<IntVec3> available = new List<IntVec3>();
        private readonly HashSet<IntVec3> availableSet = new HashSet<IntVec3>();
        private readonly List<IntVec3> itemStorageCells = new List<IntVec3>();
        private readonly List<IntVec3> rewardCells = new List<IntVec3>();

        public IReadOnlyList<IntVec3> Available => available;
        public string TemplateDefName => placement.Module.Def.defName;

        //构造函数职责：从变换后的结构掩码筛出可站立且没有实体占用的候选格。
        public GiantTombContentCellPool(Map map, GiantTombPlacement placement)
        {
            this.map = map;
            this.placement = placement;
            HashSet<IntVec3> connectorBuffer = BuildConnectorBuffer(placement);
            foreach (IntVec3 cell in GiantTombTransformUtility.StructureCells(placement))
            {
                if (!connectorBuffer.Contains(cell) && IsClearCell(cell))
                {
                    available.Add(cell);
                    availableSet.Add(cell);
                }
            }
        }

        //函数职责：随机取得并预留一个空格，空间不足时报告模板和用途。
        public IntVec3 TakeRandom(string purpose)
        {
            if (available.Count == 0)
            {
                throw NoSpace(purpose);
            }
            int index = Rand.Range(0, available.Count);
            IntVec3 cell = available[index];
            RemoveAt(index);
            return cell;
        }

        //函数职责：优先把奖励分散到蚁群储藏格和普通空格，空间用尽后复用已经选定的奖励格。
        public IntVec3 TakeRewardCell(string purpose)
        {
            List<IntVec3> unusedStorageCells = new List<IntVec3>();
            for (int i = 0; i < itemStorageCells.Count; i++)
            {
                IntVec3 cell = itemStorageCells[i];
                if (!rewardCells.Contains(cell) && CanPlaceRewardAt(cell))
                {
                    unusedStorageCells.Add(cell);
                }
            }

            if (unusedStorageCells.Count > 0)
            {
                IntVec3 storageCell = unusedStorageCells.RandomElement();
                rewardCells.Add(storageCell);
                return storageCell;
            }

            if (available.Count > 0)
            {
                IntVec3 availableCell = TakeRandom(purpose);
                rewardCells.Add(availableCell);
                return availableCell;
            }

            List<IntVec3> reusableCells = new List<IntVec3>();
            for (int i = 0; i < rewardCells.Count; i++)
            {
                if (CanPlaceRewardAt(rewardCells[i]))
                {
                    reusableCells.Add(rewardCells[i]);
                }
            }
            if (reusableCells.Count == 0)
            {
                throw NoSpace(purpose);
            }
            return reusableCells.RandomElement();
        }

        //函数职责：在指定中心附近随机取得并预留一个空格。
        public IntVec3 TakeRandomNear(IntVec3 center, float radius, string purpose)
        {
            List<IntVec3> candidates = new List<IntVec3>();
            float radiusSquared = radius * radius;
            for (int i = 0; i < available.Count; i++)
            {
                if (available[i].DistanceToSquared(center) <= radiusSquared)
                {
                    candidates.Add(available[i]);
                }
            }
            if (candidates.Count == 0)
            {
                throw NoSpace(purpose);
            }
            IntVec3 cell = candidates.RandomElement();
            Reserve(cell);
            return cell;
        }

        //函数职责：判断整个建筑占地是否仍属于当前房间的可用空格。
        public bool ContainsAll(CellRect rect)
        {
            foreach (IntVec3 cell in rect)
            {
                if (!availableSet.Contains(cell))
                {
                    return false;
                }
            }
            return true;
        }

        //函数职责：预留一个格子，防止后续内容在同一位置生成。
        public void Reserve(IntVec3 cell)
        {
            if (!availableSet.Remove(cell))
            {
                throw new InvalidOperationException("重复预留墓葬内容格: " + placement.Module.Def.defName + " @ " + cell);
            }
            available.Remove(cell);
        }

        //函数职责：把蚁群储藏位从敌人建筑候选中移除，同时保留为物品奖励落点。
        public void ReserveItemStorage(IntVec3 cell)
        {
            if (!availableSet.Remove(cell))
            {
                throw new InvalidOperationException("重复预留墓葬储藏格: " + placement.Module.Def.defName + " @ " + cell);
            }
            available.Remove(cell);
            itemStorageCells.Add(cell);
        }

        //函数职责：预留矩形内的全部格子。
        public void Reserve(CellRect rect)
        {
            foreach (IntVec3 cell in rect)
            {
                Reserve(cell);
            }
        }

        //函数职责：建立连接口本身及八向相邻格的禁用缓冲区。
        private static HashSet<IntVec3> BuildConnectorBuffer(GiantTombPlacement placement)
        {
            HashSet<IntVec3> result = new HashSet<IntVec3>();
            for (int i = 0; i < placement.Connectors.Count; i++)
            {
                List<IntVec3> cells = placement.Connectors[i].Cells;
                for (int j = 0; j < cells.Count; j++)
                {
                    foreach (IntVec3 buffered in GenRadial.RadialCellsAround(cells[j], 1.5f, true))
                    {
                        result.Add(buffered);
                    }
                }
            }
            return result;
        }

        //函数职责：确认候选格可站立且没有建筑、Pawn、植物或既有物品。
        private bool IsClearCell(IntVec3 cell)
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null || cell.GetPlant(map) != null)
            {
                return false;
            }
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                ThingCategory category = things[i].def.category;
                if (category == ThingCategory.Item || category == ThingCategory.Building || category == ThingCategory.Pawn || category == ThingCategory.Plant)
                {
                    return false;
                }
            }
            return true;
        }

        //函数职责：确认奖励格仍可容纳物品，允许同格存在已经生成的其他奖励堆。
        private bool CanPlaceRewardAt(IntVec3 cell)
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null || cell.GetPlant(map) != null)
            {
                return false;
            }
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                ThingCategory category = things[i].def.category;
                if (category == ThingCategory.Building || category == ThingCategory.Pawn || category == ThingCategory.Plant)
                {
                    return false;
                }
            }
            return true;
        }

        //函数职责：从列表和集合中同步移除一个候选格。
        private void RemoveAt(int index)
        {
            IntVec3 cell = available[index];
            available.RemoveAt(index);
            availableSet.Remove(cell);
        }

        //函数职责：创建包含模板名和生成用途的空间不足异常。
        private InvalidOperationException NoSpace(string purpose)
        {
            return new InvalidOperationException("墓葬模板可用空间不足: " + placement.Module.Def.defName + ", 内容=" + purpose);
        }
    }
}
