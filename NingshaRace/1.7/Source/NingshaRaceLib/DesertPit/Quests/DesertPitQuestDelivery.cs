using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace NingshaRaceLib.DesertPit.Quests
{
    //类职责：将地下家园的外来任务角色和物资交付到绑定地表。
    [HarmonyPatch]
    public static class DesertPitQuestDelivery
    {
        //函数职责：只解析凝砂地下家园的地表入口，避免改变其他口袋地图。
        public static Map SurfaceFor(Map map)
        {
            if (map?.Parent?.def?.defName != "NingshaRace_DesertPitHome") return map;
            Map surface = (map.Parent as PocketMapParent)?.sourceMap;
            if (surface == null || surface.IsPocketMap)
                throw new System.InvalidOperationException("凝砂地下家园没有有效的绑定地表，无法交付任务。");
            return surface;
        }

        //函数职责：在任务生成时统一聚会地点、入场和后续职责使用的地图。
        [HarmonyPostfix, HarmonyPatch(typeof(QuestGen_Get), nameof(QuestGen_Get.GetMap))]
        public static void ResolveQuestMap(ref Map __result)
        {
            __result = SurfaceFor(__result);
        }

        //函数职责：重定向实际入场节点并丢弃地下地图的局部坐标。
        [HarmonyPrefix, HarmonyPatch(typeof(QuestPart_PawnsArrive), nameof(QuestPart_PawnsArrive.Notify_QuestSignalReceived))]
        public static void BeforePawnsArrive(QuestPart_PawnsArrive __instance, Signal signal)
        {
            if (signal.tag != __instance.inSignal) return;
            Map map = __instance.mapParent?.Map;
            Map surface = SurfaceFor(map);
            if (map == surface) return;
            __instance.mapParent = surface.Parent;
            __instance.spawnNear = IntVec3.Invalid;
        }

        //函数职责：把任务奖励空投投到地表，并重新选择合法投放格。
        [HarmonyPrefix, HarmonyPatch(typeof(QuestPart_DropPods), nameof(QuestPart_DropPods.Notify_QuestSignalReceived))]
        public static void BeforeDropPods(QuestPart_DropPods __instance, Signal signal)
        {
            if (signal.tag != __instance.inSignal) return;
            Map map = __instance.mapParent?.Map;
            Map surface = SurfaceFor(map);
            if (map == surface) return;
            __instance.mapParent = surface.Parent;
            __instance.dropSpot = IntVec3.Invalid;
        }
    }
}
