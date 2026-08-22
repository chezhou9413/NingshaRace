using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Resources
{
    //类职责：在水文完成后保证生成地热与燃油资源点，并登记后续场景需要避让的建造空间。
    public sealed class GenStep_DesertPitEnergySites : GenStep
    {
        //字段职责：为能源点生成提供稳定随机种子片段。
        private const int Seed = 914027341;

        //字段职责：规定能源建筑及其周围需保留的建造半径。
        private const float ReserveRadius = 5f;

        //属性职责：向地图生成器提供能源步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：保证放置两个蒸汽喷泉和一个油砂渗洞，任一数量不足时直接报告错误。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("地下能源点");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            PlaceGuaranteedSites(map, data, ThingDefOf.SteamGeyser, 2, "蒸汽间歇喷泉");
            PlaceGuaranteedSites(map, data, DefOfRefs.NingshaRace_DesertPitOilSeep, 1, "油砂渗洞");
        }

        //函数职责：为指定自然建筑寻找完整占地与周边建造空间并登记保留区。
        private static void PlaceGuaranteedSites(Map map, DesertPitLayoutData data, ThingDef thingDef, int count, string label)
        {
            for (int i = 0; i < count; i++)
            {
                List<IntVec3> candidates = CollectCandidates(map, data, thingDef);
                if (candidates.Count == 0)
                {
                    throw new InvalidOperationException("沙漠巨坑无法生成足量" + label + "，目标数量：" + count + "。");
                }

                IntVec3 cell = candidates.RandomElementByWeight(candidate => SiteWeight(map, candidate));
                GenSpawn.Spawn(ThingMaker.MakeThing(thingDef), cell, map, Rot4.North, WipeMode.Vanish);
                ReserveArea(map, data, cell, thingDef.Size);
            }
        }

        //函数职责：收集占地完整、地面干燥并避开入口路线与既有场景的能源建筑锚点。
        private static List<IntVec3> CollectCandidates(Map map, DesertPitLayoutData data, ThingDef thingDef)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (CanPlaceSite(map, data, cell, thingDef))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        //函数职责：验证自然建筑占地及周围半径内拥有足够的可建造洞穴地面。
        private static bool CanPlaceSite(Map map, DesertPitLayoutData data, IntVec3 anchor, ThingDef thingDef)
        {
            if (anchor.DistanceTo(data.MainCenter) < 16f)
            {
                return false;
            }

            CellRect occupied = GenAdj.OccupiedRect(anchor, Rot4.North, thingDef.Size);
            foreach (IntVec3 cell in occupied)
            {
                if (!IsClearCaveCell(map, data, cell))
                {
                    return false;
                }
            }

            int clearCells = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(occupied.CenterCell, ReserveRadius, true))
            {
                if (IsClearCaveCell(map, data, cell))
                {
                    clearCells++;
                }
            }

            return clearCells >= 42;
        }

        //函数职责：判断格子是否为干燥、空置且未被路线或场景预留的天然洞穴地面。
        private static bool IsClearCaveCell(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map))
            {
                return false;
            }

            if (data.ProtectedRouteCells.Contains(cell) || data.ReservedSceneCells.Contains(cell) || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)))
            {
                return false;
            }

            if (cell.GetEdifice(map) != null || cell.GetPlant(map) != null || cell.GetFirstPawn(map) != null)
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
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

        //函数职责：按贴近宽阔洞穴区域的程度计算能源点选择权重。
        private static float SiteWeight(Map map, IntVec3 cell)
        {
            return DesertPitGenUtility.NearCaveEdge(map, cell, 4) ? 0.35f : 1f;
        }

        //函数职责：把能源建筑周围空间登记为后续蚁巢、遗迹和植物共同避让的场景保留区。
        private static void ReserveArea(Map map, DesertPitLayoutData data, IntVec3 anchor, IntVec2 size)
        {
            IntVec3 center = GenAdj.OccupiedRect(anchor, Rot4.North, size).CenterCell;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ReserveRadius, true))
            {
                if (cell.InBounds(map))
                {
                    data.ReservedSceneCells.Add(cell);
                }
            }
        }
    }
}
