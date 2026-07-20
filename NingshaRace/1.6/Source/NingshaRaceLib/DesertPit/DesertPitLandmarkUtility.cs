using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit
{
    //类职责：提供沙漠巨坑局部地貌生成所需的中心选择、地面改造和放置校验工具。
    public static class DesertPitLandmarkUtility
    {
        //字段职责：限制主洞室入口周围地貌生成，避免视觉和寻路压力集中在入口。
        private const float MainSafeRadius = 11f;

        //函数职责：收集适合成为局部地貌中心的洞穴格。
        public static List<IntVec3> CollectCenterCandidates(Map map, DesertPitLayoutData data)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!CanUseCaveCell(map, data, cell))
                {
                    continue;
                }

                if (DesertPitGenUtility.NearCaveEdge(map, cell, 5) || NearSmallRoom(data, cell, 13f) || NearCollapse(data, cell, 12f))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        //函数职责：选取一个地貌中心并移除附近中心，避免多个大地貌重叠成一团。
        public static bool TryTakeCenter(Map map, DesertPitLayoutData data, List<IntVec3> centers, float removeRadius, out IntVec3 center)
        {
            if (centers.Count == 0)
            {
                center = IntVec3.Invalid;
                return false;
            }

            center = centers.RandomElementByWeight((IntVec3 cell) => CenterWeight(map, data, cell));
            for (int i = centers.Count - 1; i >= 0; i--)
            {
                if (centers[i].DistanceTo(center) <= removeRadius)
                {
                    centers.RemoveAt(i);
                }
            }

            return true;
        }

        //函数职责：判断指定格子是否可以放置地貌装饰物或特效发射器。
        public static bool CanPlaceLandmarkThing(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            if (!CanUseCaveCell(map, data, cell) || cell.GetEdifice(map) != null || cell.GetPlant(map) != null)
            {
                return false;
            }

            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Plant)
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：在指定区域生成岩屑污迹，表现洞顶坠落和地面破碎。
        public static void ScatterRubble(Map map, IntVec3 center, float radius, int count)
        {
            int cellCount = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = center + GenRadial.RadialPattern[Rand.RangeInclusive(0, cellCount - 1)];
                if (cell.InBounds(map) && DesertPitGenUtility.IsCave(map, cell) && cell.Standable(map))
                {
                    FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_RubbleRock, Rand.RangeInclusive(1, 3));
                }
            }
        }

        //函数职责：在指定区域生成少量松散沙岩块，强化地貌边界的破碎感。
        public static void ScatterChunks(Map map, IntVec3 center, float radius, ThingDef sandstoneChunk, int count)
        {
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                if (CellFinder.TryFindRandomCellNear(center, map, Mathf.CeilToInt(radius), (IntVec3 candidate) => CanPlaceLooseThing(map, candidate), out cell))
                {
                    GenSpawn.Spawn(sandstoneChunk, cell, map);
                }
            }
        }

        //函数职责：按洞壁、小洞室和塌方位置计算地貌中心权重。
        private static float CenterWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 1f;
            if (DesertPitGenUtility.NearCaveEdge(map, cell, 5))
            {
                weight += 4f;
            }

            if (NearSmallRoom(data, cell, 13f))
            {
                weight += 2.5f;
            }

            if (NearCollapse(data, cell, 12f))
            {
                weight += 3f;
            }

            return weight;
        }

        //函数职责：判断指定格子是否满足洞穴、入口安全区和基础可放置条件。
        private static bool CanUseCaveCell(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            return cell.InBounds(map) && DesertPitGenUtility.IsCave(map, cell) && cell.Standable(map) && cell.DistanceTo(data.MainCenter) >= MainSafeRadius;
        }

        //函数职责：判断指定洞穴格是否可以放置沙岩块物品。
        private static bool CanPlaceLooseThing(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map) || cell.GetEdifice(map) != null)
            {
                return false;
            }

            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def.category == ThingCategory.Item || things[i].def.category == ThingCategory.Building)
                {
                    return false;
                }
            }

            return true;
        }

        //函数职责：判断指定格子是否靠近记录的小洞室中心。
        private static bool NearSmallRoom(DesertPitLayoutData data, IntVec3 cell, float radius)
        {
            for (int i = 0; i < data.SmallRooms.Count; i++)
            {
                if (cell.DistanceTo(data.SmallRooms[i]) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：判断指定格子是否靠近塌方和碎石边缘。
        private static bool NearCollapse(DesertPitLayoutData data, IntVec3 cell, float radius)
        {
            for (int i = 0; i < data.Collapses.Count; i++)
            {
                if (cell.DistanceTo(data.Collapses[i]) <= radius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
