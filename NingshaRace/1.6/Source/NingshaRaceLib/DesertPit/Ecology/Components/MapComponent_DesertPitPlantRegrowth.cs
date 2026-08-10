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
        private bool initialized;
        private int targetPlantCount;
        private int nextRegrowthTick;
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
            Scribe_Values.Look(ref targetPlantCount, "desertPitTargetPlantCount");
            Scribe_Values.Look(ref nextRegrowthTick, "desertPitNextRegrowthTick");
            Scribe_Collections.Look(ref habitatAnchors, "desertPitPlantHabitatAnchors", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                habitatAnchors = habitatAnchors ?? new List<IntVec3>();
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
            if (!initialized || !IsDesertPitMap() || targetPlantCount <= 0 || habitatAnchors.Count == 0)
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
            if (CountCurrentPlants(settings) < targetPlantCount)
            {
                TryRegrowOnePlant(settings);
            }
        }

        //函数职责：扫描初始植物并建立不会随后续破坏降低的生态总量上限。
        private void InitializeEcology()
        {
            DefModExtension_DesertPitEcology settings = DesertPitPlantEcologyUtility.GetSettings(map);
            habitatAnchors.Clear();
            targetPlantCount = 0;
            for (int i = 0; i < settings.plants.Count; i++)
            {
                List<Thing> plants = map.listerThings.ThingsOfDef(settings.plants[i].plant);
                for (int j = 0; j < plants.Count; j++)
                {
                    if (plants[j].Spawned)
                    {
                        targetPlantCount++;
                        habitatAnchors.Add(plants[j].Position);
                    }
                }
            }

            initialized = true;
            nextRegrowthTick = Find.TickManager.TicksGame + settings.regrowthIntervalTicks;
        }

        //函数职责：统计地图上仍然存在的全部受支持洞穴植物。
        private int CountCurrentPlants(DefModExtension_DesertPitEcology settings)
        {
            int count = 0;
            for (int i = 0; i < settings.plants.Count; i++)
            {
                count += map.listerThings.ThingsOfDef(settings.plants[i].plant).Count;
            }

            return count;
        }

        //函数职责：围绕随机初始栖息地搜索合法空格，并按原始植物权重生成一株幼株。
        private bool TryRegrowOnePlant(DefModExtension_DesertPitEcology settings)
        {
            int radialCount = GenRadial.NumCellsInRadius(settings.habitatRadius);
            for (int i = 0; i < settings.placementAttempts; i++)
            {
                IntVec3 anchor = habitatAnchors.RandomElement();
                IntVec3 cell = anchor + GenRadial.RadialPattern[Rand.Range(0, radialCount)];
                ThingDef plantDef = DesertPitPlantEcologyUtility.ChoosePlantDef(settings);
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
