using RimWorld;

using NingshaRaceLib.PocketMaps.Cargo;

namespace NingshaRaceLib.DesertPit.Buildings
{
    //类职责：作为沙漠巨坑专用离洞绳，提供返回地表与独立货运的双向传送交互。
    public sealed class Building_NingshaCaveExit : CaveExit
    {
        //属性职责：在独立货运期间显示准确的取消文案。
        public override string CancelEnterString => GetComp<Comp_NingshaPortalCargo>()?.CargoTransferActive == true
            ? "取消向地表搬运"
            : base.CancelEnterString;

        //属性职责：在独立货运期间显示准确的装载状态文案。
        public override string EnteringString => GetComp<Comp_NingshaPortalCargo>()?.CargoTransferActive == true
            ? "正在向地表搬运物资"
            : base.EnteringString;
    }
}
