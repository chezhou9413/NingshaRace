using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.DesertPit.Ecology.Config;
using NingshaRaceLib.DesertPit.Ecology.Utility;

namespace NingshaRaceLib.DesertPit.Ecology.Components
{
    //类职责：记录沙漠巨坑初始菌群容量和栖息地，并按固定间隔补生缺失植物。
    public class MapComponent_DesertPitPlantRegrowth : MapComponent
    {
        //字段职责：记录当前地图是否已经采集初始生态目标。
        private bool initialized;

        //字段职责：按植物定义保存不会随后续破坏降低的初始目标数量。
        private List<DesertPitPlantTarget> plantTargets = new List<DesertPitPlantTarget>();

        //字段职责：记录下一次尝试补生植物的游戏 Tick。
        private int nextRegrowthTick;

        //字段职责：保存初始植物位置作为后续补生搜索的栖息地锚点。
        private List<IntVec3> habitatAnchors = new List<IntVec3>();

        //构造函数职责：把洞穴菌群再生组件绑定到指定地图。
        public MapComponent_DesertPitPlantRegrowth(Map map) : base(map)
        {
        }

        //函数职责：保存菌群容量、栖息地锚点和下一次再生时间。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialized, "desertPitEcologyInitialized");
            Scribe_Collections.Look(ref plantTargets, "desertPitPlantTargets", LookMode.Deep);
            Scribe_Values.Look(ref nextRegrowthTick, "desertPitNextRegrowthTick");
            Scribe_Collections.Look(ref habitatAnchors, "desertPitPlantHabitatAnchors", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                habitatAnchors = habitatAnchors ?? new List<IntVec3>();
                plantTargets = plantTargets ?? new List<DesertPitPlantTarget>();
            }
        }

        //函数职责：新地图或旧存档首次启用时，以当前支持植物建立生态容量和栖息地锚点。
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!IsDesertPitMap() || initialized)
            {
                return;
            }

            InitializeEcology();
        }

        //函数职责：在再生计时到达后检查菌群缺口，并最多补生一株幼株。
        public override void MapComponentTick()
        {
            if (!initialized || !IsDesertPitMap() || plantTargets.Count == 0 || habitatAnchors.Count == 0)
            {
                return;
            }

            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextRegrowthTick)
            {
                return;
            }

            DefModExtension_DesertPitEcology settings = DesertPitPlantEcologyUtility.GetSettings(map);
            nextRegrowthTick = ticks + settings.regrowthIntervalTicks;
            ThingDef missingPlant = FindMostUnderrepresentedPlant();
            if (missingPlant != null)
            {
                TryRegrowOnePlant(settings, missingPlant);
            }
        }

        //函数职责：扫描初始植物并建立不会随后续破坏降低的生态总量上限。
        private void InitializeEcology()
        {
            DefModExtension_DesertPitEcology settings = DesertPitPlantEcologyUtility.GetSettings(map);
            habitatAnchors.Clear();
            plantTargets.Clear();
            for (int i = 0; i < settings.plants.Count; i++)
            {
                ThingDef plantDef = settings.plants[i].plant;
                List<Thing> plants = map.listerThings.ThingsOfDef(plantDef);
                int targetCount = 0;
                for (int j = 0; j < plants.Count; j++)
                {
                    if (plants[j].Spawned)
                    {
                        targetCount++;
                        habitatAnchors.Add(plants[j].Position);
                    }
                }

                if (targetCount > 0)
                {
                    plantTargets.Add(new DesertPitPlantTarget(plantDef, targetCount));
                }
            }

            initialized = true;
            nextRegrowthTick = Find.TickManager.TicksGame + settings.regrowthIntervalTicks;
        }

        //函数职责：按照缺失比例选择最需要恢复的植物，防止食药菌被装饰种永久替代。
        private ThingDef FindMostUnderrepresentedPlant()
        {
            ThingDef result = null;
            float largestMissingRatio = 0f;
            for (int i = 0; i < plantTargets.Count; i++)
            {
                DesertPitPlantTarget target = plantTargets[i];
                if (target.PlantDef == null || target.TargetCount <= 0)
                {
                    continue;
                }

                int current = map.listerThings.ThingsOfDef(target.PlantDef).Count;
                float missingRatio = Mathf.Max(0f, target.TargetCount - current) / target.TargetCount;
                if (missingRatio > largestMissingRatio)
                {
                    largestMissingRatio = missingRatio;
                    result = target.PlantDef;
                }
            }

            return result;
        }

        //函数职责：围绕随机初始栖息地搜索合法空格，并补生指定的缺失植物幼株。
        private bool TryRegrowOnePlant(DefModExtension_DesertPitEcology settings, ThingDef plantDef)
        {
            int radialCount = GenRadial.NumCellsInRadius(settings.habitatRadius);
            for (int i = 0; i < settings.placementAttempts; i++)
            {
                IntVec3 anchor = habitatAnchors.RandomElement();
                IntVec3 cell = anchor + GenRadial.RadialPattern[Rand.Range(0, radialCount)];
                if (DesertPitPlantEcologyUtility.CanRegrowPlantAt(map, cell, plantDef))
                {
                    DesertPitPlantEcologyUtility.SpawnPlant(map, plantDef, cell, settings.initialGrowthRange);
                    return true;
                }
            }

            return false;
        }

        //函数职责：判断当前地图是否使用带有菌群再生配置的沙漠巨坑生物群系。
        private bool IsDesertPitMap()
        {
            return DesertPitPlantEcologyUtility.GetSettings(map) != null;
        }
    }
}
