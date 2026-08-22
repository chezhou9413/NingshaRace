using NingshaRaceLib.PocketMaps.Buildings;
using NingshaRaceLib.PocketMaps.Cargo;

namespace NingshaRaceLib.DesertPit.Buildings
{
    //类职责：作为凝砂族沙漠巨坑入口，沿用原版口袋地图入口交互并指向自定义地下沙岩洞穴。
    public class Building_DesertPitGate : Building_NingshaPocketMapPortal
    {
        //属性职责：在独立货运期间显示准确的取消文案。
        public override string CancelEnterString => GetComp<Comp_NingshaPortalCargo>()?.CargoTransferActive == true
            ? "取消向地下搬运"
            : base.CancelEnterString;

        //属性职责：在独立货运期间显示准确的装载状态文案。
        public override string EnteringString => GetComp<Comp_NingshaPortalCargo>()?.CargoTransferActive == true
            ? "正在向地下搬运物资"
            : base.EnteringString;
    }
}
