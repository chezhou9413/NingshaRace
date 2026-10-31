using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation
{
    //只在生成和建成事件铺设菌膜，动画交给地形 Shader，不逐格运行 Tick。
    internal static class AntColonyGround
    {
        //铺满核心生活区，用连续角度扰动形成不规则外缘，并保留场景通道。
        public static void Generate(Map map, IntVec3 center, List<IntVec3> buildArea, List<IntVec3> storage)
        {
            HashSet<IntVec3> allowed = new HashSet<IntVec3>(buildArea);
            HashSet<IntVec3> candidates = new HashSet<IntVec3>();
            float phase = (center.x * 13 + center.z * 7 + map.uniqueID * 19) * 0.17f;
            foreach (IntVec3 cell in allowed)
            {
                float angle = Mathf.Atan2(cell.z - center.z, cell.x - center.x);
                float radius = 10.2f + Mathf.Sin(angle * 3f + phase) * 0.65f + Mathf.Sin(angle * 7f - phase) * 0.4f;
                if ((cell.DistanceToSquared(center) <= radius * radius || storage.Contains(cell)) && CanCover(map, cell, true)) candidates.Add(cell);
            }
            Queue<IntVec3> open = new Queue<IntVec3>();
            if (candidates.Remove(center)) open.Enqueue(center);
            while (open.Count > 0)
            {
                IntVec3 cell = open.Dequeue();
                map.terrainGrid.SetTerrain(cell, DefOfRefs.NingshaRace_AntGround);
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                    if (candidates.Remove(cell + offset)) open.Enqueue(cell + offset);
            }
        }

        //附属建筑建成时覆盖其占地及紧邻边缘，不把远处房间整体铺满。
        public static void CoverBuilding(Map map, IntVec3 cell, IntVec2 size, List<IntVec3> buildArea)
        {
            foreach (IntVec3 covered in GenAdj.OccupiedRect(cell, Rot4.North, size).ExpandedBy(1))
                if (buildArea.Contains(covered) && CanCover(map, covered))
                    map.terrainGrid.SetTerrain(covered, DefOfRefs.NingshaRace_AntGround);
        }

        //场景生成时覆盖墓葬原有地砖；后续建造只扩展自然地面，保留玩家地板。
        private static bool CanCover(Map map, IntVec3 cell, bool generating = false) => cell.InBounds(map) && cell.Standable(map)
            && cell.GetEdifice(map) == null && (generating || cell.GetTerrain(map).natural) && !cell.GetTerrain(map).IsWater;
    }
}
