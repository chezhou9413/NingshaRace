using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.Ecology.Config;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Ecology.Utility
{
    //类职责：为洞穴植物初始生成和运行时再生提供统一的配置、权重、放置和生成方法。
    public static class DesertPitPlantEcologyUtility
    {
        private const float ExitSafeRadius = 8f;
        private const float AntNestSafeRadius = 10f;

        //函数职责：取得地图所属沙漠巨坑生物群系上的生态配置。
        public static DefModExtension_DesertPitEcology GetSettings(Map map)
        {
            return map?.Biome?.GetModExtension<DefModExtension_DesertPitEcology>();
        }

        //函数职责：判断指定植物是否属于沙漠巨坑允许再生的配置植物池。
        public static bool IsSupportedPlant(ThingDef plantDef, DefModExtension_DesertPitEcology settings)
        {
            if (plantDef == null || settings?.plants == null)
            {
                return false;
            }

            for (int i = 0; i < settings.plants.Count; i++)
            {
                if (settings.plants[i].plant == plantDef)
                {
                    return true;
                }
            }

            return false;
        }

        //函数职责：按照生态配置权重选择一种可生成的洞穴植物。
        public static ThingDef ChoosePlantDef(DefModExtension_DesertPitEcology settings)
        {
            if (settings?.plants == null || settings.plants.Count == 0)
            {
                throw new System.InvalidOperationException("沙漠巨坑生态配置没有可用植物。");
            }

            DesertPitPlantWeight record = settings.plants.RandomElementByWeight((DesertPitPlantWeight entry) => Mathf.Max(0f, entry.weight));
            if (record.plant == null)
            {
                throw new System.InvalidOperationException("沙漠巨坑生态配置包含空植物引用。");
            }

            return record.plant;
        }

        //函数职责：检查格子是否满足初始生成和运行时再生共同需要的基础生长条件。
        public static bool CanPlacePlant(Map map, IntVec3 cell, ThingDef plantDef, bool rejectPlayerAreas)
        {
            if (map == null || plantDef == null || !cell.InBounds(map) || !cell.Standable(map))
            {
                return false;
            }

            RoofDef roof = cell.GetRoof(map);
            if (roof == null || !roof.isNatural || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)))
            {
                return false;
            }

            if (cell.GetPlant(map) != null || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null)
            {
                return false;
            }

            if (rejectPlayerAreas && (map.zoneManager.ZoneAt(cell) != null || map.areaManager.Home[cell]))
            {
                return false;
            }

            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Plant || thing is Blueprint || thing is Frame)
                {
                    return false;
                }
            }

            return plantDef.CanEverPlantAt(cell, map, false, false);
        }

        //函数职责：检查运行时再生格是否同时避开出口、蚁巢保护区和实体储藏格。
        public static bool CanRegrowPlantAt(Map map, IntVec3 cell, ThingDef plantDef)
        {
            if (!CanPlacePlant(map, cell, plantDef, true))
            {
                return false;
            }

            if (NearThingDef(map, cell, DefOfRefs.NingshaRace_DesertPitCaveExit, ExitSafeRadius) || NearThingDef(map, cell, DefOfRefs.NingshaRace_DesertPitAntNest, AntNestSafeRadius))
            {
                return false;
            }

            return !map.GetComponent<MapComponent_DesertPitAntColonies>().IsColonyStorageCell(cell);
        }

        //函数职责：在指定格子生成一株处于给定成长率范围内的洞穴植物。
        public static Plant SpawnPlant(Map map, ThingDef plantDef, IntVec3 cell, FloatRange growthRange)
        {
            Plant plant = (Plant)ThingMaker.MakeThing(plantDef);
            plant.Growth = Mathf.Clamp01(growthRange.RandomInRange);
            return (Plant)GenSpawn.Spawn(plant, cell, map);
        }

        //函数职责：判断指定格子是否位于某类场景实体的安全避让半径内。
        private static bool NearThingDef(Map map, IntVec3 cell, ThingDef thingDef, float radius)
        {
            List<Thing> things = map.listerThings.ThingsOfDef(thingDef);
            for (int i = 0; i < things.Count; i++)
            {
                if (!things[i].Destroyed && things[i].Position.DistanceTo(cell) < radius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
