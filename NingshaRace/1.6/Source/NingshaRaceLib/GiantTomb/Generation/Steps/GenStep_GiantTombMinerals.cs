using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation.Steps
{
    //类职责：按原版山地密度、团块尺寸和常见度在墓葬外砂岩中散布基础矿物。
    public sealed class GenStep_GiantTombMinerals : GenStep_ScatterLumpsMineable
    {
        //字段职责：限制随机矿脉只使用原版七种基础可开采资源。
        private static readonly string[] AllowedMineables =
        {
            "MineableSteel",
            "MineableComponentsIndustrial",
            "MineableSilver",
            "MineableGold",
            "MineableUranium",
            "MineablePlasteel",
            "MineableJade"
        };

        //字段职责：缓存已经解析的基础矿物Def，避免每个矿脉团块重复查询数据库和分配列表。
        private static List<ThingDef> allowedMineableDefs;

        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 197428373;

        //函数职责：设置原版山地矿脉密度后调用原版团块散布流程。
        public override void Generate(Map map, GenStepParams parms)
        {
            countPer10kCellsRange = new FloatRange(15f, 15f);
            useNomadicMineables = true;
            base.Generate(map, parms);
        }

        //函数职责：按ThingDef自身的原版常见度从允许矿物中选择一种团块。
        protected override ThingDef ChooseThingDef()
        {
            if (allowedMineableDefs == null)
            {
                allowedMineableDefs = AllowedMineables.Select((string defName) => DefDatabase<ThingDef>.GetNamed(defName)).ToList();
            }
            return allowedMineableDefs.RandomElementByWeight((ThingDef def) => def.building.mineableScatterCommonality);
        }
    }
}
