using System.Reflection;
using HarmonyLib;
using NingshaRaceLib.DesertPit.Ecology.Plants;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Patches
{
    //统一限制洞穴植物的播种和自然生成地面，保留原版占用与环境检查。
    [HarmonyPatch]
    internal static class Patch_DesertPitPlantTerrain
    {
        //选择返回详细种植报告的重载，布尔入口也会经过这里。
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PlantUtility), nameof(PlantUtility.CanEverPlantAt),
                new[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(Thing).MakeByRefType(),
                    typeof(bool), typeof(bool), typeof(bool) });
        }

        //仅处理本模组洞穴植物，不放宽地板、水面或其他植物的种植条件。
        private static void Postfix(ThingDef plantDef, IntVec3 c, Map map, bool writeNoReason,
            ref AcceptanceReport __result)
        {
            if (!__result.Accepted || !typeof(Plant_DesertPit).IsAssignableFrom(plantDef.thingClass)) return;
            if (!Plant_DesertPit.CanGrowOn(c.GetTerrain(map)))
            {
                if (writeNoReason) __result = false;
                else __result = "需要沙地、沙砾、土地或粗糙岩石地面。";
            }
        }
    }
}
