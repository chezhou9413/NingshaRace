using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Steps
{
    //类职责：在沙漠巨坑洞穴中生成浅层地下溪流、积水池和湿润边缘地形。
    public class GenStep_DesertPitHydrology : GenStep
    {
        //字段职责：记录当前生成步骤使用的稳定随机种子片段。
        private const int Seed = 914027340;

        //字段职责：限制主洞室入口周围水体生成，避免水体干扰出口区域。
        private const float MainSafeRadius = 12f;

        //字段职责：提供溪流寻路时可选择的八方向偏移。
        private static readonly IntVec3[] Directions =
        {
            new IntVec3(1, 0, 0),
            new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1),
            new IntVec3(0, 0, -1),
            new IntVec3(1, 0, 1),
            new IntVec3(1, 0, -1),
            new IntVec3(-1, 0, 1),
            new IntVec3(-1, 0, -1)
        };

        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => Seed;

        //函数职责：按洞室锚点生成溪流和积水池，并让边缘过渡为湿地或沼泽。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("地下水系");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            TerrainDef waterMoving = DefDatabase<TerrainDef>.GetNamed("WaterMovingShallow");
            TerrainDef waterShallow = DefDatabase<TerrainDef>.GetNamed("WaterShallow");
            TerrainDef marsh = DefDatabase<TerrainDef>.GetNamed("Marsh");
            TerrainDef marshy = DefDatabase<TerrainDef>.GetNamed("MarshyTerrain");
            List<IntVec3> anchors = CollectAnchors(map, data);
            if (anchors.Count == 0)
            {
                return;
            }

            PaintStreams(map, data, anchors, waterMoving, waterShallow, marshy);
            PaintPools(map, data, anchors, waterShallow, marsh, marshy);
        }

        //函数职责：收集小洞室、塌方边缘和洞壁附近适合形成水文地貌的锚点。
        private static List<IntVec3> CollectAnchors(Map map, DesertPitLayoutData data)
        {
            List<IntVec3> anchors = new List<IntVec3>();
            AddRecordedAnchors(map, data, data.SmallRooms, anchors);
            AddRecordedAnchors(map, data, data.Collapses, anchors);
            int attempts = Mathf.Min(map.Size.x * map.Size.z / 45, 520);
            for (int i = 0; i < attempts && anchors.Count < 24; i++)
            {
                IntVec3 cell = CellFinder.RandomCell(map);
                if (CanPaintWater(map, data, cell) && DesertPitGenUtility.NearCaveEdge(map, cell, 5) && Rand.Chance(0.42f))
                {
                    anchors.Add(cell);
                }
            }

            return anchors;
        }

        //函数职责：把布局记录点转换成附近可落水的真实洞穴格。
        private static void AddRecordedAnchors(Map map, DesertPitLayoutData data, List<IntVec3> source, List<IntVec3> anchors)
        {
            for (int i = 0; i < source.Count; i++)
            {
                IntVec3 cell;
                if (CellFinder.TryFindRandomCellNear(source[i], map, 8, (IntVec3 candidate) => CanPaintWater(map, data, candidate), out cell))
                {
                    anchors.Add(cell);
                }
            }
        }

        //函数职责：在远端洞室锚点之间生成一到两条弯曲浅流。
        private static void PaintStreams(Map map, DesertPitLayoutData data, List<IntVec3> anchors, TerrainDef waterMoving, TerrainDef waterShallow, TerrainDef marshy)
        {
            int streamCount = anchors.Count >= 7 && Rand.Chance(0.25f) ? 2 : 1;
            for (int i = 0; i < streamCount; i++)
            {
                IntVec3 start = anchors.RandomElement();
                IntVec3 end = FarthestAnchor(start, anchors);
                if (!end.IsValid || start.DistanceTo(end) < 24f)
                {
                    continue;
                }

                List<IntVec3> path = BuildStreamPath(map, data, start, end);
                if (path.Count >= 12)
                {
                    PaintStreamPath(map, data, path, waterMoving, waterShallow, marshy);
                }
            }
        }

        //函数职责：选择距离起点最远的水文锚点作为溪流终点。
        private static IntVec3 FarthestAnchor(IntVec3 start, List<IntVec3> anchors)
        {
            IntVec3 result = IntVec3.Invalid;
            float bestDistance = 0f;
            for (int i = 0; i < anchors.Count; i++)
            {
                float distance = anchors[i].DistanceTo(start);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    result = anchors[i];
                }
            }

            return result;
        }

        //函数职责：沿洞穴可通行空间生成带有轻微摆动的溪流中心线。
        private static List<IntVec3> BuildStreamPath(Map map, DesertPitLayoutData data, IntVec3 start, IntVec3 end)
        {
            List<IntVec3> path = new List<IntVec3>();
            HashSet<IntVec3> visited = new HashSet<IntVec3>();
            IntVec3 current = start;
            int maxSteps = Mathf.CeilToInt(start.DistanceTo(end) * 3.4f);
            for (int i = 0; i < maxSteps && current.IsValid; i++)
            {
                path.Add(current);
                visited.Add(current);
                if (current.DistanceTo(end) <= 2f)
                {
                    break;
                }

                current = ChooseNextStep(map, data, current, end, visited);
            }

            return path;
        }

        //函数职责：从当前格周围选择更接近终点且仍位于洞穴内的下一步。
        private static IntVec3 ChooseNextStep(Map map, DesertPitLayoutData data, IntVec3 current, IntVec3 end, HashSet<IntVec3> visited)
        {
            IntVec3 result = IntVec3.Invalid;
            float bestScore = float.MinValue;
            for (int i = 0; i < Directions.Length; i++)
            {
                IntVec3 candidate = current + Directions[i];
                if (!CanPaintWater(map, data, candidate) || visited.Contains(candidate))
                {
                    continue;
                }

                float score = -candidate.DistanceTo(end);
                score += DesertPitGenUtility.NearCaveEdge(map, candidate, 4) ? 1.4f : 0f;
                score += Rand.Range(-1.1f, 1.1f);
                if (score > bestScore)
                {
                    bestScore = score;
                    result = candidate;
                }
            }

            return result;
        }

        //函数职责：围绕溪流中心线铺设流动浅水、静水肩部和湿润边缘。
        private static void PaintStreamPath(Map map, DesertPitLayoutData data, List<IntVec3> path, TerrainDef waterMoving, TerrainDef waterShallow, TerrainDef marshy)
        {
            float width = Rand.Range(1.35f, 2.15f);
            for (int i = 0; i < path.Count; i++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(path[i], width + 1.2f, useCenter: true))
                {
                    if (!CanPaintWater(map, data, cell))
                    {
                        continue;
                    }

                    float distance = cell.DistanceTo(path[i]);
                    if (distance <= 0.75f)
                    {
                        map.terrainGrid.SetTerrain(cell, waterMoving);
                    }
                    else if (distance <= width)
                    {
                        map.terrainGrid.SetTerrain(cell, waterShallow);
                    }
                    else if (Rand.Chance(0.45f))
                    {
                        map.terrainGrid.SetTerrain(cell, marshy);
                    }
                }
            }
        }

        //函数职责：在洞室低洼处生成数个不规则积水池。
        private static void PaintPools(Map map, DesertPitLayoutData data, List<IntVec3> anchors, TerrainDef waterShallow, TerrainDef marsh, TerrainDef marshy)
        {
            int poolCount = Mathf.Min(Rand.RangeInclusive(3, 5), anchors.Count);
            for (int i = 0; i < poolCount; i++)
            {
                IntVec3 center = anchors.RandomElement();
                float radius = Rand.Range(3.8f, 7.2f);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius + 2.2f, useCenter: true))
                {
                    PaintPoolCell(map, data, center, radius, cell, waterShallow, marsh, marshy);
                }
            }
        }

        //函数职责：根据离积水中心的距离铺设浅水、沼泽和湿地边缘。
        private static void PaintPoolCell(Map map, DesertPitLayoutData data, IntVec3 center, float radius, IntVec3 cell, TerrainDef waterShallow, TerrainDef marsh, TerrainDef marshy)
        {
            if (!CanPaintWater(map, data, cell))
            {
                return;
            }

            float distance = cell.DistanceTo(center);
            float edgeNoise = Rand.Range(-0.6f, 0.9f);
            if (distance <= radius * 0.52f + edgeNoise)
            {
                map.terrainGrid.SetTerrain(cell, waterShallow);
            }
            else if (distance <= radius * 0.82f + edgeNoise)
            {
                map.terrainGrid.SetTerrain(cell, marsh);
            }
            else if (distance <= radius + 1.8f && Rand.Chance(0.55f))
            {
                map.terrainGrid.SetTerrain(cell, marshy);
            }
        }

        //函数职责：判断指定格子是否允许被改造成水体或湿地。
        private static bool CanPaintWater(Map map, DesertPitLayoutData data, IntVec3 cell)
        {
            if (!cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell) || !cell.Standable(map) || cell.DistanceTo(data.MainCenter) < MainSafeRadius)
            {
                return false;
            }

            if (cell.GetEdifice(map) != null || cell.GetPlant(map) != null)
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
    }
}
