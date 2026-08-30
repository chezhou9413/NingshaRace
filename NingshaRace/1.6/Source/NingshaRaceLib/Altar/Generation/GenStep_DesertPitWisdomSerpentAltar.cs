using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Ecology.Generation;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.Altar.Generation
{
    //类职责：在每张新沙漠巨坑的能源点之后保证放置一座智慧之蛇祭坛。
    public sealed class GenStep_DesertPitWisdomSerpentAltar : GenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 182430517;

        //函数职责：筛选远离入口、路线、水域、实体与保留区的洞穴格并生成唯一祭坛。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            HashSet<IntVec3> reachableCells = CollectReachableCells(map, data.MainCenter);
            List<IntVec3> candidates = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanPlaceAt(map, data, reachableCells, cell))
                {
                    candidates.Add(cell);
                }
            }
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("沙漠巨坑没有找到可生成智慧之蛇祭坛的合法位置。");
            }
            IntVec3 chosen = candidates.RandomElementByWeight(cell => PlacementWeight(data, cell));
            GenSpawn.Spawn(ThingMaker.MakeThing(DefOfRefs.NingshaRace_Altar), chosen, map, Rot4.North);
            DesertPitCaveEcologyUtility.ReserveScene(map, data, chosen, 4f);
        }

        //函数职责：验证祭坛位于入口可达且距离适中的干燥洞穴，并靠近但不占用主通行路线。
        private static bool CanPlaceAt(Map map, DesertPitLayoutData data, HashSet<IntVec3> reachableCells, IntVec3 cell)
        {
            float entranceDistance = cell.DistanceTo(data.MainCenter);
            if (!cell.InBounds(map) || entranceDistance < 22f || entranceDistance > 50f
                || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map)
                || !reachableCells.Contains(cell) || !IsNearProtectedRoute(data, cell)
                || data.ProtectedRouteCells.Contains(cell) || data.ReservedSceneCells.Contains(cell)
                || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)))
            {
                return false;
            }
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                ThingCategory category = things[i].def.category;
                if (category == ThingCategory.Item || category == ThingCategory.Building
                    || category == ThingCategory.Pawn || category == ThingCategory.Plant)
                {
                    return false;
                }
            }
            return true;
        }

        //函数职责：从巨坑入口沿所有可站立格洪泛，取得玩家无需破墙即可到达的完整区域。
        private static HashSet<IntVec3> CollectReachableCells(Map map, IntVec3 entrance)
        {
            HashSet<IntVec3> reachable = new HashSet<IntVec3>();
            map.floodFiller.FloodFill(entrance, cell => cell.InBounds(map) && cell.Standable(map),
                cell => { reachable.Add(cell); });
            return reachable;
        }

        //函数职责：要求祭坛处在主洞网路线八格以内，同时保留路线格本身的通行空间。
        private static bool IsNearProtectedRoute(DesertPitLayoutData data, IntVec3 cell)
        {
            foreach (IntVec3 nearby in GenRadial.RadialCellsAround(cell, 8f, true))
            {
                if (data.ProtectedRouteCells.Contains(nearby))
                {
                    return true;
                }
            }
            return false;
        }

        //函数职责：优先选择小洞室附近且不贴近洞壁的位置作为祭坛场景。
        private static float PlacementWeight(DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 1f / (1f + Math.Abs(cell.DistanceTo(data.MainCenter) - 32f) * 0.08f);
            for (int i = 0; i < data.SmallRooms.Count; i++)
            {
                if (cell.DistanceTo(data.SmallRooms[i]) <= 8f)
                {
                    weight += 8f;
                }
            }
            return weight;
        }
    }
}
