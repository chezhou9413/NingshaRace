using RimWorld;
using Verse;

using NingshaRaceLib.DesertPit.Utility;

namespace NingshaRaceLib.DesertPit.Placement
{
    //类职责：阻止玩家在完全覆盖厚岩顶的沙漠巨坑中放置太阳能板。
    public sealed class PlaceWorker_RejectSolarInDesertPit : PlaceWorker
    {
        //函数职责：仅对凝砂沙漠巨坑返回无日照拒绝原因，其他地图保持原版判定。
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (DesertPitMapUtility.IsDesertPitMap(map))
            {
                return "沙漠巨坑的厚岩顶无法接收日照";
            }

            return AcceptanceReport.WasAccepted;
        }
    }
}
