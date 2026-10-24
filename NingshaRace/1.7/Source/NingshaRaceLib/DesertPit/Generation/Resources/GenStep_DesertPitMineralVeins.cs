using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Resources
{
    //类职责：把沙漠巨坑的零散矿物替换为贴近已探索洞穴且向岩层内部延伸的连续矿脉。
    public sealed class GenStep_DesertPitMineralVeins : GenStep
    {
        //字段职责：为矿脉生成提供稳定随机种子片段。
        private const int Seed = 914027340;

        //属性职责：向地图生成器提供矿脉步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：生成三条钢铁、一条机械和一条按权重选择的高级矿脉。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("连续矿脉");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            HashSet<IntVec3> occupied = new HashSet<IntVec3>();

            for (int i = 0; i < 3; i++)
            {
                GenerateVein(map, data, ThingDefOf.MineableSteel, Rand.RangeInclusive(10, 16), occupied, "钢铁矿脉");
            }

            GenerateVein(map, data, ThingDefOf.MineableComponentsIndustrial, Rand.RangeInclusive(5, 8), occupied, "压缩机械矿脉");
            GenerateVein(map, data, ChooseAdvancedMineable(), Rand.RangeInclusive(4, 7), occupied, "高级矿脉");
        }

        //函数职责：按塑钢、铀和黄金的既定权重选择高级矿脉种类。
        private static ThingDef ChooseAdvancedMineable()
        {
            float value = Rand.Value;
            if (value < 0.45f)
            {
                return DefOfRefs.MineablePlasteel;
            }

            return value < 0.75f ? DefOfRefs.MineableUranium : DefOfRefs.MineableGold;
        }

        //函数职责：寻找可见沙岩边缘并构造达到指定格数的连续矿脉，失败时报告生成错误。
        private static void GenerateVein(Map map, DesertPitLayoutData data, ThingDef mineable, int count, HashSet<IntVec3> occupied, string label)
        {
            List<IntVec3> seeds = CollectExposedSeeds(map, data, occupied);
            seeds.Shuffle();
            for (int i = 0; i < seeds.Count; i++)
            {
                List<IntVec3> cells;
                if (TryGrowVein(map, data, seeds[i], count, occupied, out cells))
                {
                    for (int j = 0; j < cells.Count; j++)
                    {
                        cells[j].GetEdifice(map).Destroy(DestroyMode.Vanish);
                        GenSpawn.Spawn(mineable, cells[j], map);
                        occupied.Add(cells[j]);
                    }

                    return;
                }
            }

            throw new InvalidOperationException("沙漠巨坑无法生成" + label + "，目标格数：" + count + "。");
        }

        //函数职责：收集至少有一侧暴露于已挖开洞穴的天然沙岩墙作为矿脉起点。
        private static List<IntVec3> CollectExposedSeeds(Map map, DesertPitLayoutData data, HashSet<IntVec3> occupied)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanUseWall(map, data, cell, occupied) && AdjacentToCave(map, cell))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        //函数职责：从暴露边缘开始沿四向天然沙岩墙扩张并保持整条矿脉连通。
        private static bool TryGrowVein(Map map, DesertPitLayoutData data, IntVec3 seed, int count, HashSet<IntVec3> occupied, out List<IntVec3> cells)
        {
            cells = new List<IntVec3> { seed };
            HashSet<IntVec3> chosen = new HashSet<IntVec3> { seed };
            List<IntVec3> frontier = new List<IntVec3>();
            AddFrontier(map, data, seed, occupied, chosen, frontier);

            while (cells.Count < count && frontier.Count > 0)
            {
                IntVec3 next = frontier.RandomElementByWeight(candidate => AdjacentToCave(map, candidate) ? 0.35f : 2f);
                frontier.Remove(next);
                if (!chosen.Add(next))
                {
                    continue;
                }

                cells.Add(next);
                AddFrontier(map, data, next, occupied, chosen, frontier);
            }

            return cells.Count == count;
        }

        //函数职责：把指定矿格周围仍可使用的四向沙岩墙加入扩张边界。
        private static void AddFrontier(Map map, DesertPitLayoutData data, IntVec3 center, HashSet<IntVec3> occupied, HashSet<IntVec3> chosen, List<IntVec3> frontier)
        {
            for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
            {
                IntVec3 cell = center + GenAdj.CardinalDirections[i];
                if (!chosen.Contains(cell) && !frontier.Contains(cell) && CanUseWall(map, data, cell, occupied))
                {
                    frontier.Add(cell);
                }
            }
        }

        //函数职责：判断墙格是否是远离入口路线、场景保留区和既有矿脉的天然沙岩。
        private static bool CanUseWall(Map map, DesertPitLayoutData data, IntVec3 cell, HashSet<IntVec3> occupied)
        {
            if (!cell.InBounds(map) || occupied.Contains(cell) || data.ProtectedRouteCells.Contains(cell) || data.ReservedSceneCells.Contains(cell))
            {
                return false;
            }

            if (cell.DistanceTo(data.MainCenter) < 12f || NearOccupiedVein(cell, occupied))
            {
                return false;
            }

            Building wall = cell.GetEdifice(map);
            return wall != null && wall.def == ThingDefOf.Sandstone;
        }

        //函数职责：判断候选格是否紧邻其他矿脉，保持各条资源带彼此独立。
        private static bool NearOccupiedVein(IntVec3 cell, HashSet<IntVec3> occupied)
        {
            foreach (IntVec3 existing in occupied)
            {
                if (cell.DistanceToSquared(existing) <= 9f)
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：判断沙岩墙是否紧邻已挖开的可站立洞穴，使矿脉至少暴露一个边缘。
        private static bool AdjacentToCave(Map map, IntVec3 cell)
        {
            for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
            {
                IntVec3 adjacent = cell + GenAdj.CardinalDirections[i];
                if (adjacent.InBounds(map) && DesertPitGenUtility.IsCave(map, adjacent) && adjacent.Standable(map))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
