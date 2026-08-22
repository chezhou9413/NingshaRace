using Verse;

namespace NingshaRaceLib.PocketMaps.Cargo
{
    //类职责：为凝砂族口袋地图传送门声明独立货运组件。
    public sealed class CompProperties_NingshaPortalCargo : CompProperties
    {
        //函数职责：将组件属性绑定到凝砂族跨地图货运逻辑。
        public CompProperties_NingshaPortalCargo()
        {
            compClass = typeof(Comp_NingshaPortalCargo);
        }
    }
}
