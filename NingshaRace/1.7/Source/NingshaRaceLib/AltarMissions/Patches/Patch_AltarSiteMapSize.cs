using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.AltarMissions.Patches
{
    //类职责：为专用祭坛Site覆盖原版固定二百格偏好尺寸，使三类任务使用锁定的小地图尺寸。
    [HarmonyPatch(typeof(Site), nameof(Site.PreferredMapSize), MethodType.Getter)]
    public static class Patch_AltarSiteMapSize
    {
        //函数职责：按祭坛世界地点Def把地图尺寸改为小型遗迹一百二十、蚁巢一百四十或解救一百二十。
        public static void Postfix(Site __instance, ref IntVec3 __result)
        {
            string name = __instance.def?.defName;
            if (name == "NingshaRace_AltarAntNestSite")
            {
                __result = new IntVec3(140, 1, 140);
            }
            else if (name == "NingshaRace_AltarSmallRuinsSite" || name == "NingshaRace_AltarRescueSurfaceSite"
                || name == "NingshaRace_AltarRescueUndergroundSite")
            {
                __result = new IntVec3(120, 1, 120);
            }
        }
    }
}
