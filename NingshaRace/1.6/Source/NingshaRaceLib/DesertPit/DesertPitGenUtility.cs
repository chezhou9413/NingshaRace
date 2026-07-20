using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NingshaRaceLib.DesertPit
{
    //类职责：提供沙漠巨坑各个生成步骤共享的临时数据、洞穴掩码和安全清理工具。
    public static class DesertPitGenUtility
    {
        //字段职责：保存沙漠巨坑布局数据在 MapGenerator 临时表里的键名。
        public const string LayoutDataKey = "NingshaRace_DesertPitLayoutData";

        //字段职责：保存洞穴边缘噪声在 MapGenerator 临时表里的键名。
        private const string EdgeNoiseKey = "NingshaRace_DesertPitEdgeNoise";

        //函数职责：取得当前生成流程里的沙漠巨坑布局数据。
        public static DesertPitLayoutData GetLayoutData()
        {
            return MapGenerator.GetOrGenerateVar<DesertPitLayoutData>(LayoutDataKey);
        }

        //函数职责：创建用于洞室和隧道边缘扰动的 Perlin 噪声。
        public static ModuleBase CreateEdgeNoise()
        {
            ModuleBase noise = MapGenerator.GetVar<ModuleBase>(EdgeNoiseKey);
            if (noise != null)
            {
                return noise;
            }

            noise = new Perlin(0.055000000819563866, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);
            MapGenerator.SetVar(EdgeNoiseKey, noise);
            return noise;
        }

        //函数职责：判断指定格子是否属于沙漠巨坑已挖开的洞穴空间。
        public static bool IsCave(IntVec3 cell)
        {
            return MapGenerator.Caves[cell] > 0f;
        }

        //函数职责：判断指定格子在地图内且属于已挖开的洞穴空间。
        public static bool IsCave(Map map, IntVec3 cell)
        {
            return cell.InBounds(map) && MapGenerator.Caves[cell] > 0f;
        }

        //函数职责：把指定格子标记为洞穴并保留较高的洞穴强度。
        public static void MarkCave(IntVec3 cell, Map map, float strength)
        {
            if (!cell.InBounds(map))
            {
                return;
            }

            MapGenerator.Caves[cell] = Mathf.Max(MapGenerator.Caves[cell], strength);
        }

        //函数职责：按半径把一片格子标记为洞穴，中心强度更高、边缘强度更低。
        public static void CarveCircle(Map map, Vector3 center, float radius, float strength)
        {
            IntVec3 root = center.ToIntVec3();
            int cellCount = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < cellCount; i++)
            {
                IntVec3 cell = root + GenRadial.RadialPattern[i];
                if (!cell.InBounds(map))
                {
                    continue;
                }

                float distance = Vector3.Distance(cell.ToVector3Shifted(), center);
                float edgeStrength = Mathf.Clamp01(1f - distance / Mathf.Max(radius, 0.1f));
                MarkCave(cell, map, Mathf.Max(strength * edgeStrength, 0.35f));
            }
        }

        //函数职责：清理指定区域内会阻挡入口和主路的物体并铺成沙地。
        public static void ClearSafeArea(Map map, IntVec3 center, float radius)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (thing.def.destroyable)
                    {
                        thing.Destroy();
                    }
                }

                MarkCave(cell, map, 1f);
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Sand);
            }
        }

        //函数职责：判断格子附近是否紧邻洞穴边界。
        public static bool NearCaveEdge(Map map, IntVec3 cell, int radius)
        {
            int cellCount = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < cellCount; i++)
            {
                IntVec3 check = cell + GenRadial.RadialPattern[i];
                if (check.InBounds(map) && !IsCave(map, check))
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：判断指定地形是否属于水面、浅流、沼泽或湿地边缘。
        public static bool IsWaterLikeTerrain(TerrainDef terrain)
        {
            return terrain.defName == "WaterShallow" || terrain.defName == "WaterMovingShallow" || terrain.defName == "Marsh" || terrain.defName == "MarshyTerrain";
        }

        //函数职责：更新地图生成长任务窗口中的当前阶段提示。
        public static void SetGenerationStatus(string stage)
        {
            LongEventHandler.SetCurrentEventText("正在生成凝砂沙漠巨坑：" + stage);
        }
    }
}
