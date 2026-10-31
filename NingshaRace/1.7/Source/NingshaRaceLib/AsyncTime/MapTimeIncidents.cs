using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.AsyncTime;

//将地图事件按目标分流；世界任务本身仍由全局 QuestManager 推进。
[HarmonyPatch, HarmonyPatchCategory(MapTimePatches.PatchCategory)]
internal static class MapTimeIncidents
{
    private static readonly AccessTools.FieldRef<IncidentQueue, List<QueuedIncident>> Queue =
        AccessTools.FieldRefAccess<IncidentQueue, List<QueuedIncident>>("queuedIncidents");
    private static readonly AccessTools.FieldRef<QueuedIncident, int> FireTick =
        AccessTools.FieldRefAccess<QueuedIncident, int>("fireTick");

    //启用时当地时钟与世界相同，已有事件可直接移入当地队列。
    internal static void MoveQueuedIncidents(MapComponent_NingshaTime clock)
    {
        var source = Queue(Current.Game.storyteller.incidentQueue);
        for (int i = source.Count - 1; i >= 0; i--)
        {
            if (source[i].FiringIncident.parms.target != clock.map) continue;
            clock.Storyteller.incidentQueue.Add(source[i]);
            source.RemoveAt(i);
        }
    }

    //当地事件按当地 Tick 检查，不重复执行涉及世界的任务部件。
    internal static void Tick(MapComponent_NingshaTime clock)
    {
        RimWorld.Storyteller local = clock.Storyteller;
        RimWorld.Storyteller global = Current.Game.storyteller;
        if (local.def != global.def)
        {
            local.def = global.def;
            AccessTools.Method(typeof(RimWorld.Storyteller), "InitializeStorytellerComps").Invoke(local, null);
        }
        local.difficultyDef = global.difficultyDef;
        local.difficulty = global.difficulty;
        local.incidentQueue.IncidentQueueTick();
        if (clock.Ticks % RimWorld.Storyteller.CheckInterval != 0 || !DebugSettings.enableStoryteller) return;
        var targets = new List<IIncidentTarget> { clock.map };
        foreach (StorytellerComp comp in local.storytellerComps)
            foreach (FiringIncident incident in local.MakeIncidentsForInterval(comp, targets))
                local.TryFire(incident);
    }

    //世界叙事者只对仍跟随世界时间的地图生成事件。
    [HarmonyPostfix, HarmonyPatch(typeof(RimWorld.Storyteller), nameof(RimWorld.Storyteller.AllIncidentTargets), MethodType.Getter)]
    private static void FilterTargets(ref List<IIncidentTarget> __result)
    {
        if (MapTimeScope.Current is { } clock)
        {
            __result = new List<IIncidentTarget> { clock.map };
            return;
        }
        if (!__result.Exists(target => target is Map map && NingshaTime.ActiveClock(map) != null)) return;
        //复制结果，避免修改原版复用列表影响嵌套事件生成。
        __result = __result.FindAll(target => target is not Map map || NingshaTime.ActiveClock(map) == null);
    }

    //按调用方的当前时间换算截止点，进入当地队列后保留原版重试规则。
    [HarmonyPrefix, HarmonyPatch(typeof(IncidentQueue), nameof(IncidentQueue.Add), typeof(QueuedIncident))]
    private static bool RouteQueue(IncidentQueue __instance, QueuedIncident qi, ref bool __result)
    {
        if (qi.FiringIncident.parms.target is not Map map) return true;
        var clock = NingshaTime.ActiveClock(map);
        if (clock == null || __instance == clock.Storyteller.incidentQueue) return true;
        FireTick(qi) += clock.Ticks - Find.TickManager.TicksGame;
        __result = clock.Storyteller.incidentQueue.Add(qi);
        return false;
    }

    //外部立即事件转为当地下一 Tick 执行，暂停期间不会生成敌人。
    [HarmonyPrefix, HarmonyPatch(typeof(RimWorld.Storyteller), nameof(RimWorld.Storyteller.TryFire))]
    private static bool RouteImmediate(FiringIncident fi, bool queued, ref bool __result)
    {
        if (fi.parms.target is not Map map) return true;
        var clock = NingshaTime.ActiveClock(map);
        if (clock == null || TickingLocally(clock)) return true;
        clock.Storyteller.incidentQueue.Add(new QueuedIncident(fi, clock.Ticks + 1));
        __result = true;
        return false;
    }

    //只让当地调度真正执行事件，其他上下文的 TryFire 都只入队。
    private static bool TickingLocally(MapComponent_NingshaTime clock) => MapTimePatches.TickingMap == clock.map;
}
