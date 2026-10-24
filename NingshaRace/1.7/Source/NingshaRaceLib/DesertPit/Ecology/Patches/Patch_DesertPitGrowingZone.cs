using HarmonyLib;
using NingshaRaceLib.DesertPit.Ecology.Plants;
using NingshaRaceLib.DesertPit.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Patches
{
    //巨坑内允许在适合洞穴菌类的零肥力地面划种植区。
    [HarmonyPatch(typeof(Designator_ZoneAdd_Growing), nameof(Designator_ZoneAdd_Growing.CanDesignateCell))]
    internal static class Patch_DesertPitGrowingZone
    {
        //保留区域类型、迷雾、地图边缘和实体占用限制。
        private static void Postfix(Designator_ZoneAdd_Growing __instance, IntVec3 c,
            ref AcceptanceReport __result)
        {
            Map map = __instance.Map;
            if (__result.Accepted || !DesertPitMapUtility.IsDesertPitMap(map) || !c.InBounds(map)
                || !Plant_DesertPit.CanGrowOn(c.GetTerrain(map))) return;
            Zone zone = map.zoneManager.ZoneAt(c);
            if (zone != null && zone.GetType() != typeof(Zone_Growing)) return;
            __result = Designator_ZoneAdd.IsZoneableCell(c, map);
        }
    }
}
