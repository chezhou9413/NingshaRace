using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Components
{
    //类职责：持久化单种洞穴植物在地图初始生态中的目标数量。
    public sealed class DesertPitPlantTarget : IExposable
    {
        //字段职责：记录需要维持数量的植物定义。
        public ThingDef PlantDef;

        //字段职责：记录该植物在地图生成完成时的初始目标数量。
        public int TargetCount;

        //函数职责：供存档系统通过无参构造函数建立目标记录。
        public DesertPitPlantTarget()
        {
        }

        //函数职责：用植物定义和初始数量建立目标记录。
        public DesertPitPlantTarget(ThingDef plantDef, int targetCount)
        {
            PlantDef = plantDef;
            TargetCount = targetCount;
        }

        //函数职责：保存植物定义与对应的初始目标数量。
        public void ExposeData()
        {
            Scribe_Defs.Look(ref PlantDef, "plantDef");
            Scribe_Values.Look(ref TargetCount, "targetCount", 0);
        }
    }
}
