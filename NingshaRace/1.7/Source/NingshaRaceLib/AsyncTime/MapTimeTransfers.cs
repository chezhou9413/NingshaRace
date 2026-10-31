using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace NingshaRaceLib.AsyncTime;

//在生成、装载和离图边界维护物体的时钟归属；未附着物体使用世界时间。
[HarmonyPatch, HarmonyPatchCategory(MapTimePatches.PatchCategory)]
internal static class MapTimeTransfers
{
    //弱引用记录不阻止地图清理；读档时从所属地图重建。
    private sealed class ClockOwner
    {
        internal Map Map;
    }

    private static readonly ConditionalWeakTable<Thing, ClockOwner> Owners = new();

    //记录地图中的根物体及容器内容，现有时间戳不发生变化。
    internal static void RememberMap(Map map)
    {
        var things = new List<Thing>();
        ThingOwnerUtility.GetAllThingsRecursively(map, things, false);
        foreach (Thing thing in things) Owners.GetOrCreateValue(thing).Map = map;
    }

    //容器整体换图时一次处理全部真实内容，避免背包或运输舱中的人物遗漏。
    private static List<Thing> Tree(Thing thing)
    {
        var result = new List<Thing>();
        if (thing is IThingHolder holder) ThingOwnerUtility.GetAllThingsRecursively(holder, result, false);
        if (!result.Contains(thing)) result.Add(thing);
        return result;
    }

    //已经知晓的来源优先于当前上下文；离图后立即转成世界时间。
    private static void Transfer(Thing thing, Map destination, Map fallbackSource = null)
    {
        if (thing.Destroyed || Scribe.mode != LoadSaveMode.Inactive) return;
        destination = NingshaTime.ActiveClock(destination)?.map;
        fallbackSource = NingshaTime.ActiveClock(fallbackSource)?.map;
        if (destination == null && fallbackSource == null &&
            (!Owners.TryGetValue(thing, out var rootOwner) || rootOwner.Map == null)) return;
        foreach (Thing child in Tree(thing))
        {
            Map source = Owners.TryGetValue(child, out var owner) ? owner.Map : fallbackSource;
            if (source == destination) continue;
            int delta = NingshaTime.GetTicks(destination) - NingshaTime.GetTicks(source);
            MapTimePawnStamps.Shift(child, delta);
            Owners.GetOrCreateValue(child).Map = destination;
            if (delta != 0) NingshaTime.NotifyTimeShifted(child, delta);
        }
    }

    //新物体的初始化使用当前逻辑时钟，先记录来源以便随后生成到其他地图。
    [HarmonyPostfix, HarmonyPatch(typeof(ThingMaker), nameof(ThingMaker.MakeThing))]
    private static void Made(Thing __result)
    {
        if (__result != null && MapTimeScope.Current != null)
            Owners.GetOrCreateValue(__result).Map = MapTimeScope.Current.map;
    }

    //容器接纳物体时立即换算，暂停地图无需等到恢复才能确定归属。
    [HarmonyPrefix, HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
    private static void Added(ThingOwner __instance, Thing item)
    {
        if (Current.ProgramState != ProgramState.Playing || Scribe.mode != LoadSaveMode.Inactive) return;
        Transfer(item, ThingOwnerUtility.GetRootMap(__instance.Owner));
    }

    //地图根容器的移除交给 DeSpawn 末尾；其他容器移除后使用世界时间。
    [HarmonyPostfix, HarmonyPatch(typeof(ThingOwner), "NotifyRemoved")]
    private static void Removed(ThingOwner __instance, Thing item)
    {
        if (__instance.Owner is Map || Current.ProgramState != ProgramState.Playing) return;
        Transfer(item, null, ThingOwnerUtility.GetRootMap(__instance.Owner));
    }

    //离图前保留来源，原版注销时仍能定位旧地图 TickList。
    [HarmonyPrefix, HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    private static void BeforeDespawn(Thing __instance, out Map __state) => __state = __instance.Map;

    //非人物物体在基类离图结束时转换时间。
    [HarmonyPostfix, HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    private static void AfterDespawn(Thing __instance, Map __state)
    {
        if (__instance is not Pawn && !__instance.Spawned) Transfer(__instance, null, __state);
    }

    //人物需要等作业和人物组件的离图处理全部结束。
    [HarmonyPrefix, HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
    private static void BeforePawnDespawn(Pawn __instance, out Map __state) => __state = __instance.Map;

    //把人物及其装备、背包、携带对象一起转成世界时间。
    [HarmonyPostfix, HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
    private static void AfterPawnDespawn(Pawn __instance, Map __state)
    {
        if (!__instance.Spawned) Transfer(__instance, null, __state);
    }

    //生成全过程使用目标地图时钟，包含派生类 SpawnSetup 与组件初始化。
    [HarmonyPrefix, HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), typeof(Thing), typeof(IntVec3),
        typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool))]
    private static void BeforeSpawn(Map map, bool respawningAfterLoad, out MapTimeScope __state)
    {
        __state = !respawningAfterLoad && UnityData.IsInMainThread && Current.ProgramState == ProgramState.Playing
            && (MapTimeScope.InScope || NingshaTime.ActiveClock(map) != null)
            ? new MapTimeScope(map) : null;
    }

    //无论生成成功与否都恢复调用地图。
    [HarmonyFinalizer, HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), typeof(Thing), typeof(IntVec3),
        typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool))]
    private static void AfterSpawn(MapTimeScope __state) => __state?.Dispose();
}
