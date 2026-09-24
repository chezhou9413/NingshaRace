using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.DesertPit.Ecology.Config;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Ecology.Habitats;

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

        //字段职责：保存普通菌群共用的初始栖息地，巨菇位置由各物种目标独立保存。
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

        //函数职责：在地图首次初始化时，以实际生成植物建立各类生态容量和栖息地锚点。
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
            if (!initialized || plantTargets.Count == 0)
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
            foreach (DesertPitPlantTarget target in FindUnderrepresentedPlants())
            {
                //某类栖息地被建造占用时继续检查其他缺失种类，每轮总计仍只补一株。
                if (TryRegrowOnePlant(settings, target)) break;
            }
        }

        //函数职责：扫描初始植物并建立不会随后续破坏降低的生态总量上限。
        private void InitializeEcology()
        {
            DefModExtension_DesertPitEcology settings = DesertPitPlantEcologyUtility.GetSettings(map);
            plantTargets.Clear();
            habitatAnchors.Clear();
            foreach (DesertPitPlantWeight entry in settings.plants) RegisterTarget(entry.plant, false);
            foreach (DesertPitGiantFungus entry in settings.giantFungi) RegisterTarget(entry.plant, true);

            initialized = true;
            nextRegrowthTick = Find.TickManager.TicksGame + settings.regrowthIntervalTicks;
        }

        //函数职责：记录温床外的实际数量，普通菌群共享位置而巨菇分别保留原生位置。
        private void RegisterTarget(ThingDef plantDef, bool giant)
        {
            List<IntVec3> anchors = new List<IntVec3>();
            MapComponent_AntHabitats habitats = map.GetComponent<MapComponent_AntHabitats>();
            foreach (Thing plant in map.listerThings.ThingsOfDef(plantDef))
                if (plant.Spawned && !habitats.IsFungalHabitat(plant.Position)) anchors.Add(plant.Position);
            if (anchors.Count > 0)
                plantTargets.Add(new DesertPitPlantTarget(plantDef, anchors.Count, giant ? anchors : null));
            if (!giant) habitatAnchors.AddRange(anchors);
        }

        //函数职责：按缺失比例降序返回待补生种类，保持各物种自己的数量上限。
        private List<DesertPitPlantTarget> FindUnderrepresentedPlants()
        {
            List<KeyValuePair<DesertPitPlantTarget, float>> missing = new List<KeyValuePair<DesertPitPlantTarget, float>>();
            MapComponent_AntHabitats habitats = map.GetComponent<MapComponent_AntHabitats>();
            for (int i = 0; i < plantTargets.Count; i++)
            {
                DesertPitPlantTarget target = plantTargets[i];
                if (target.PlantDef == null || target.TargetCount <= 0)
                {
                    continue;
                }

                int current = 0;
                foreach (Thing plant in map.listerThings.ThingsOfDef(target.PlantDef))
                    if (plant.Spawned && !habitats.IsFungalHabitat(plant.Position)) current++;
                float missingRatio = Mathf.Max(0f, target.TargetCount - current) / target.TargetCount;
                if (missingRatio > 0f) missing.Add(new KeyValuePair<DesertPitPlantTarget, float>(target, missingRatio));
            }
            missing.Sort((a, b) => b.Value.CompareTo(a.Value));
            List<DesertPitPlantTarget> result = new List<DesertPitPlantTarget>();
            foreach (var entry in missing) result.Add(entry.Key);
            return result;
        }

        //函数职责：围绕初始栖息地补生幼株，巨菇使用同类锚点并满足开阔度与树木间距。
        private bool TryRegrowOnePlant(DefModExtension_DesertPitEcology settings, DesertPitPlantTarget target)
        {
            DesertPitGiantFungus fungus = DesertPitGiantFungusUtility.Find(settings, target.PlantDef);
            List<IntVec3> anchors = fungus == null ? habitatAnchors : target.GiantHabitatAnchors;
            int radialCount = GenRadial.NumCellsInRadius(settings.habitatRadius);
            for (int i = 0; i < settings.placementAttempts; i++)
            {
                IntVec3 anchor = anchors.RandomElement();
                IntVec3 cell = anchor + GenRadial.RadialPattern[Rand.Range(0, radialCount)];
                if (!map.GetComponent<MapComponent_AntHabitats>().IsFungalHabitat(cell)
                    && DesertPitPlantEcologyUtility.CanRegrowPlantAt(map, cell, target.PlantDef)
                    && (fungus == null || DesertPitGiantFungusUtility.HasSpace(map, cell, fungus)))
                {
                    DesertPitPlantEcologyUtility.SpawnPlant(map, target.PlantDef, cell, settings.initialGrowthRange);
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
