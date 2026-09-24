using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Components
{
    //类职责：持久化单种洞穴植物的初始数量，并为巨菇保存独立栖息地。
    public sealed class DesertPitPlantTarget : IExposable
    {
        //字段职责：记录需要维持数量的植物定义。
        public ThingDef PlantDef;

        //字段职责：记录该植物在地图生成完成时的初始目标数量。
        public int TargetCount;

        //字段职责：保存同类巨菇的初始位置，普通菌群继续使用地图共享栖息地。
        public List<IntVec3> GiantHabitatAnchors;

        //函数职责：供存档系统通过无参构造函数建立目标记录。
        public DesertPitPlantTarget()
        {
        }

        //函数职责：用实际数量建立目标，巨菇同时提供同类初始位置。
        public DesertPitPlantTarget(ThingDef plantDef, int targetCount, List<IntVec3> giantHabitatAnchors = null)
        {
            PlantDef = plantDef;
            TargetCount = targetCount;
            GiantHabitatAnchors = giantHabitatAnchors;
        }

        //函数职责：保存植物定义、初始目标数量及巨菇的同类栖息地锚点。
        public void ExposeData()
        {
            Scribe_Defs.Look(ref PlantDef, "plantDef");
            Scribe_Values.Look(ref TargetCount, "targetCount", 0);
            Scribe_Collections.Look(ref GiantHabitatAnchors, "giantHabitatAnchors", LookMode.Value);
        }
    }
}
