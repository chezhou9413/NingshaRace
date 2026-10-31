using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.AsyncTime;

//将已接管地图从原版全局调度中移出，保留世界与未接管地图的原版入口。
[HarmonyPatch, HarmonyPatchCategory(PatchCategory)]
internal static class MapTimePatches
{
    internal const string PatchCategory = "NingshaAsyncTime";
    private static readonly Harmony RuntimeHarmony = new("chezhou.race.ningsharace.asynctime");
    private static bool patchesInstalled;
    internal static Map TickingMap;

    //仅启用后逐帧重置当地步数；不再有请求时撤下整个异步时序模块。
    [HarmonyPrefix, HarmonyPatch(typeof(TickManager), nameof(TickManager.TickManagerUpdate))]
    private static void BeginFrame()
    {
        bool needsPatches = false;
        foreach (Map map in Find.Maps)
        {
            var clock = map.GetComponent<MapComponent_NingshaTime>();
            clock.FrameTicks = 0;
            needsPatches |= clock.Requested;
        }
        SetPatchesEnabled(needsPatches);
    }

    //安全边界内迁移队列，不在物体更新循环中删除元素。
    [HarmonyPrefix, HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    private static void PrepareMaps()
    {
        foreach (Map map in Find.Maps) map.GetComponent<MapComponent_NingshaTime>().Prepare();
    }

    //世界推进结束后逐张推进当地地图，异常地图暂停并明确记录原因。
    [HarmonyPostfix, HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    private static void TickMaps()
    {
        var maps = Find.Maps.ToArray();
        foreach (Map map in maps)
        {
            var clock = NingshaTime.ActiveClock(map);
            if (clock == null || map.Disposed) continue;
            try { clock.Advance(); }
            catch (Exception exception)
            {
                clock.SetRate(0f);
                Log.Error($"[凝砂时序] 地图 {map.uniqueID} 推进失败，已暂停该地图：{exception}");
            }
        }
    }

    //当地管理器只允许由对应地图的调度器调用。
    [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.MapPreTick))]
    private static bool PreTick(Map __instance) => CanTick(__instance);

    //阻止世界 Tick 再次更新当地天气、电力与地图组件。
    [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.MapPostTick))]
    private static bool PostTick(Map __instance) => CanTick(__instance);

    //无独立时钟的地图完全沿用原版。
    private static bool CanTick(Map map) => NingshaTime.ActiveClock(map) == null || TickingMap == map;

    //物体出生时进入地图自己的队列。
    [HarmonyPrefix, HarmonyPatch(typeof(TickManager), nameof(TickManager.RegisterAllTickabilityFor))]
    private static bool Register(Thing t)
    {
        var clock = NingshaTime.ActiveClock(t.Map);
        if (clock == null) return true;
        clock.Register(t);
        return false;
    }

    //物体离开时从同一队列注销。
    [HarmonyPrefix, HarmonyPatch(typeof(TickManager), nameof(TickManager.DeRegisterAllTickabilityFor))]
    private static bool Deregister(Thing t)
    {
        var clock = NingshaTime.ActiveClock(t.Map);
        if (clock == null) return true;
        clock.Deregister(t);
        return false;
    }

    //首次请求时挂载整个分类，不再使用时连同转移、事件、环境与查询补丁一起卸载。
    internal static void SetPatchesEnabled(bool enabled)
    {
        if (patchesInstalled == enabled) return;
        var assembly = typeof(MapTimePatches).Assembly;
        if (enabled) RuntimeHarmony.PatchCategory(assembly, PatchCategory);
        else RuntimeHarmony.UnpatchCategory(assembly, PatchCategory);
        patchesInstalled = enabled;
    }

    //跨图后旧队列可能尚未处理注销，禁止物体被旧地图重复推进。
    [HarmonyPrefix, HarmonyPatch(typeof(Thing), nameof(Thing.DoTick))]
    private static bool GuardThing(Thing __instance)
    {
        Map map = __instance.MapHeld;
        if (TickingMap != null) return map == TickingMap;
        return NingshaTime.ActiveClock(map) == null;
    }

    //绘制仍照常执行，地图查询使用冻结或加速后的当地时间。
    [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.MapUpdate))]
    private static void BeginUpdate(Map __instance, out MapTimeScope __state)
    {
        var clock = NingshaTime.ActiveClock(__instance);
        __state = clock == null ? null : new MapTimeScope(__instance);
        if (clock != null) MapTimeScope.SetFrameTicks(clock.FrameTicks);
    }

    //绘制异常同样恢复世界时钟。
    [HarmonyFinalizer, HarmonyPatch(typeof(Map), nameof(Map.MapUpdate))]
    private static void EndUpdate(MapTimeScope __state) => __state?.Dispose();

    //暂停只影响处于该地图上下文中的动画与查询。
    [HarmonyPostfix, HarmonyPatch(typeof(TickManager), nameof(TickManager.Paused), MethodType.Getter)]
    private static void Paused(ref bool __result)
    {
        if (MapTimeScope.Current?.Rate == 0f) __result = true;
    }

    //原版人物插值采用世界速度乘当地倍率。
    [HarmonyPostfix, HarmonyPatch(typeof(TickManager), nameof(TickManager.TickRateMultiplier), MethodType.Getter)]
    private static void Rate(ref float __result)
    {
        if (MapTimeScope.Current != null) __result *= MapTimeScope.Current.Rate;
    }

    //地图内事件沿用独立叙事记录，其他调用仍返回游戏叙事者。
    [HarmonyPostfix, HarmonyPatch(typeof(Find), nameof(Find.Storyteller), MethodType.Getter)]
    private static void Storyteller(ref RimWorld.Storyteller __result)
    {
        if (MapTimeScope.Current?.Storyteller != null) __result = MapTimeScope.Current.Storyteller;
    }
}
