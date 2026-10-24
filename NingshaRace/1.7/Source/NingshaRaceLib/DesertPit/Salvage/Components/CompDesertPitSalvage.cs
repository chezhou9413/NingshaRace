using Verse;

namespace NingshaRaceLib.DesertPit.Salvage.Components
{
    //类职责：在洞穴物件被玩家拆除后抽取并生成一次专用资源奖励。
    public sealed class CompDesertPitSalvage : ThingComp
    {
        //属性职责：提供当前洞穴物件的拆除奖励配置。
        private CompProperties_DesertPitSalvage Props => (CompProperties_DesertPitSalvage)props;

        //函数职责：只在正常拆除完成后生成一项互斥奖励，其他销毁方式不产出资源。
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (mode != DestroyMode.Deconstruct)
            {
                return;
            }
            if (previousMap == null)
            {
                Log.Error("[NingshaRace] 洞穴物件拆除后缺少原地图，无法生成回收资源：" + parent.def.defName);
                return;
            }

            DesertPitSalvageOption option = Props.options.RandomElementByWeight(candidate => candidate.weight);
            Thing reward = ThingMaker.MakeThing(option.thingDef);
            reward.stackCount = option.count;
            if (GenPlace.TryPlaceThing(reward, parent.Position, previousMap, ThingPlaceMode.Near))
            {
                return;
            }

            reward.Destroy(DestroyMode.Vanish);
            Log.Error("[NingshaRace] 无法在洞穴物件拆除位置附近生成回收资源：" + option.thingDef.defName);
        }
    }
}
