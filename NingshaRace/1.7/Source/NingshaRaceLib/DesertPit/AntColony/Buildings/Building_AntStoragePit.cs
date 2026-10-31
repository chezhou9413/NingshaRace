using HarmonyLib;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Buildings
{
    //储物穴只冷藏内部物品，不改变周围房间温度。
    public sealed class Building_AntStoragePit : Building_AntColonyStructure
    {
        //仅储物穴显示库存槽位；原版说明为空时不添加开头换行。
        public override string GetInspectString() => InspectPrefix() + "库存：" + Contents.Count + " / " + Settings.capacity
            + " 堆" + (FoodOnly ? "\n优先存粮" : "") + "\n内部温度：4℃";
    }

    //接入原版持有物温度查询，腐烂仍由 CompRottable 正常计算。
    [HarmonyPatch(typeof(ThingOwnerUtility), nameof(ThingOwnerUtility.TryGetFixedTemperature))]
    public static class Patch_AntStorageTemperature
    {
        //只处理储物穴，其余持有者沿用原版。
        public static bool Prefix(IThingHolder holder, ref float temperature, ref bool __result)
        {
            if (!(holder is Building_AntStoragePit)) return true;
            temperature = 4f;
            __result = true;
            return false;
        }
    }
}
