using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Compatibility
{
    //兼容床位归属模组的全地图缓存，避免失效床位影响休息、死亡和人物生成。
    [HarmonyPatch]
    internal static class SensibleBedOwnershipPatch
    {
        //没有启用床位模组时不安装此补丁。
        private static bool Prepare() => AccessTools.TypeByName("SensibleBedOwnership.Utility") != null;

        //只替换床位集合查询，沿用该模组的归属与首选床位规则。
        private static MethodBase TargetMethod() => AccessTools.Method(
            "SensibleBedOwnership.Utility:AllAssignedBedsAndDeathrestCaskets");

        //从原版建筑列表重建缓存，无床地图自然返回空集合；查询前剔除已卸载地图的床。
        private static bool Prefix(Pawn pawn, ref List<Building_Bed> __result,
            ref HashSet<Building_Bed> ___bedCache, ref bool ___bedCacheDirty)
        {
            if (___bedCacheDirty)
            {
                ___bedCache = new HashSet<Building_Bed>();
                foreach (Map map in Find.Maps)
                    foreach (Building building in map.listerBuildings.allBuildingsColonist)
                        if (building is Building_Bed bed) ___bedCache.Add(bed);
                ___bedCacheDirty = false;
            }
            ___bedCache.RemoveWhere(bed => bed == null || !bed.Spawned
                || !Find.Maps.Contains(bed.Map) || bed.CompAssignableToPawn == null);
            __result = new List<Building_Bed>();
            foreach (Building_Bed bed in ___bedCache)
                if (bed.CompAssignableToPawn.AssignedPawnsForReading.Contains(pawn)) __result.Add(bed);
            return false;
        }
    }
}
