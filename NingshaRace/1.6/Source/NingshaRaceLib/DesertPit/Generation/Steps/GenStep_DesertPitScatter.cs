using System.Collections.Generic;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Steps
{
    //类职责：在沙漠巨坑小洞室、洞壁过渡带和塌方区域散布沙岩块和岩屑。
    public class GenStep_DesertPitScatter : GenStep
    {
        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => 914027334;

        //函数职责：围绕小洞室生成岩块，并在洞穴边缘生成岩屑和松散沙岩块。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("岩屑矿脉");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            ThingDef sandstoneChunk = DefOfRefs.NingshaRace_DesertPitSandstoneRubbleSmall;
            foreach (IntVec3 room in data.SmallRooms)
            {
                ScatterChunks(map, room, sandstoneChunk);
            }

            ScatterRubbleFields(map, data);
            ScatterLooseStoneChips(map, data, sandstoneChunk);
        }

        //函数职责：在洞室边缘生成较多沙岩块，形成岩屑堆积的骨架。
        private static void ScatterChunks(Map map, IntVec3 room, ThingDef sandstoneChunk)
        {
            int count = Rand.RangeInclusive(8, 15);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                if (CellFinder.TryFindRandomCellNear(room, map, 9, (IntVec3 candidate) => CanPlaceLooseThing(map, candidate) && DesertPitGenUtility.NearCaveEdge(map, candidate, 3), out cell))
                {
                    GenSpawn.Spawn(sandstoneChunk, cell, map);
                }
            }
        }

        //函数职责：在洞壁、塌方和砂岩地面附近生成岩石碎屑痕迹。
        private static void ScatterRubbleFields(Map map, DesertPitLayoutData data)
        {
            List<IntVec3> candidates = CollectRubbleCandidates(map, data);
            int targetCount = Rand.RangeInclusive(180, 260);
            int count = 0;
            int guard = 0;
            while (count < targetCount && candidates.Count > 0 && guard < 1200)
            {
                IntVec3 cell = candidates.RandomElementByWeight((IntVec3 candidate) => RubbleWeight(map, data, candidate));
                candidates.Remove(cell);
                if (FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_RubbleRock, Rand.RangeInclusive(1, 2)))
                {
                    count++;
                }

                guard++;
            }
        }

        //函数职责：在岩屑带中额外生成少量可见沙岩块，强化洞穴破碎感。
        private static void ScatterLooseStoneChips(Map map, DesertPitLayoutData data, ThingDef sandstoneChunk)
        {
            List<IntVec3> candidates = CollectRubbleCandidates(map, data);
            int targetCount = Rand.RangeInclusive(24, 42);
            int count = 0;
            int guard = 0;
            while (count < targetCount && candidates.Count > 0 && guard < 500)
            {
                IntVec3 cell = candidates.RandomElementByWeight((IntVec3 candidate) => ChunkWeight(map, data, candidate));
                candidates.Remove(cell);
                if (!CanPlaceLooseThing(map, cell))
                {
                    guard++;
                    continue;
                }

                GenSpawn.Spawn(sandstoneChunk, cell, map);
                RemoveNearbyCandidates(candidates, cell, 3.5f);
                count++;
                guard++;
            }
        }

        //函数职责：收集适合生成岩屑和松散沙岩块的洞穴格。
        private static List<IntVec3> CollectRubbleCandidates(Map map, DesertPitLayoutData data)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map))
                {
                    continue;
                }

                if (cell.DistanceTo(data.MainCenter) < 8f || cell.GetEdifice(map) != null || cell.GetPlant(map) != null)
                {
                    continue;
                }

                result.Add(cell);
            }

            return result;
        }

        //函数职责：计算岩屑痕迹生成权重，让碎屑沿洞壁、塌方和砂岩地面聚集。
        private static float RubbleWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 0.4f;
            if (DesertPitGenUtility.NearCaveEdge(map, cell, 4))
            {
                weight += 4f;
            }

            if (NearCollapse(data, cell))
            {
                weight += 3.5f;
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain.defName == "Sandstone_Rough")
            {
                weight += 2f;
            }
            else if (terrain == TerrainDefOf.Gravel)
            {
                weight += 1.5f;
            }

            return weight;
        }

        //函数职责：计算松散沙岩块生成权重，让实体岩块比岩屑更贴近洞壁和塌方点。
        private static float ChunkWeight(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            float weight = 0.2f;
            if (DesertPitGenUtility.NearCaveEdge(map, cell, 3))
            {
                weight += 5f;
            }

            if (NearCollapse(data, cell))
            {
                weight += 4f;
            }

            if (cell.GetTerrain(map).defName == "Sandstone_Rough")
            {
                weight += 1.5f;
            }

            return weight;
        }

        //函数职责：移除已放置沙岩块周围的候选格，避免大块岩石挤成一团。
        private static void RemoveNearbyCandidates(List<IntVec3> candidates, IntVec3 placed, float radius)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].DistanceTo(placed) < radius)
                {
                    candidates.RemoveAt(i);
                }
            }
        }

        //函数职责：判断指定洞穴格是否可以放置可拆除的松散岩块建筑。
        private static bool CanPlaceLooseThing(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            return DesertPitGenUtility.IsCave(cell) && cell.Standable(map) && cell.GetEdifice(map) == null;
        }

        //函数职责：判断指定格子是否靠近塌方和碎石边缘。
        private static bool NearCollapse(DesertPitLayoutData data, IntVec3 cell)
        {
            for (int i = 0; i < data.Collapses.Count; i++)
            {
                if (cell.DistanceTo(data.Collapses[i]) <= 10f)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
