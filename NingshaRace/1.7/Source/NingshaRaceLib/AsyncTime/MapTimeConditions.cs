using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.AsyncTime;

//地图环境状态读取当地时间，世界环境状态始终读取世界时间。
[HarmonyPatch, HarmonyPatchCategory(MapTimePatches.PatchCategory)]
internal static class MapTimeConditions
{
    //环境状态面板即使在地图绘制之外查询，也能得到正确的剩余时间。
    [HarmonyPostfix, HarmonyPatch(typeof(GameCondition), nameof(GameCondition.TicksPassed), MethodType.Getter)]
    private static void Passed(GameCondition __instance, ref int __result)
    {
        if (__instance.gameConditionManager != null)
            __result = NingshaTime.GetTicks(__instance.gameConditionManager.ownerMap) - __instance.startTick;
    }

    //保留永久状态语义，避免当地暂停后仍按世界时间到期。
    [HarmonyPostfix, HarmonyPatch(typeof(GameCondition), nameof(GameCondition.Expired), MethodType.Getter)]
    private static void Expired(GameCondition __instance, ref bool __result)
    {
        if (__instance.gameConditionManager != null)
            __result = !__instance.Permanent && __instance.TicksPassed > __instance.Duration;
    }

    //注册环境状态时，将开始时间换算到目标地图再运行原版初始化。
    [HarmonyPrefix, HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.RegisterCondition))]
    private static void BeginRegister(GameConditionManager __instance, GameCondition cond, out MapTimeScope __state)
    {
        __state = null;
        if (!UnityData.IsInMainThread || Current.ProgramState != ProgramState.Playing) return;
        int difference = NingshaTime.GetTicks(__instance.ownerMap) - Find.TickManager.TicksGame;
        cond.startTick += difference;
        __state = new MapTimeScope(__instance.ownerMap);
    }

    //初始化失败不留下当地时间上下文。
    [HarmonyFinalizer, HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.RegisterCondition))]
    private static void EndRegister(MapTimeScope __state) => __state?.Dispose();
}
