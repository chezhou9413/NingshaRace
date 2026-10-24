using System;
using HarmonyLib;
using Verse;

namespace NingshaRaceLib.AsyncTime;

//提供按地图启用的时间控制；所有调用均在游戏主线程完成。
public static class NingshaTime
{
    public const float MaximumRate = 15f;

    //通知跨地图或容器移动的物体换算自定义时间戳，偏移量为目标时间减来源时间。
    public static event Action<Thing, int> ThingTimeShifted;

    //倍率相对于世界 Tick；零表示暂停，首次调用在下一个世界 Tick 前接管地图。
    public static void SetRate(Map map, float rate)
    {
        CheckMap(map);
        if (!float.IsFinite(rate) || rate < 0f || rate > MaximumRate)
            throw new ArgumentOutOfRangeException(nameof(rate), "地图时间倍率必须在 0 到 15 之间。");
        if (AccessTools.TypeByName("Multiplayer.Client.AsyncTimeComp") != null)
            throw new InvalidOperationException("凝砂异步时序不能与 Multiplayer 的地图调度器同时启用。");
        map.GetComponent<MapComponent_NingshaTime>().SetRate(rate);
    }

    //暂停当地模拟，不积累恢复时需要补算的 Tick。
    public static void Pause(Map map) => SetRate(map, 0f);

    //恢复暂停前的非零倍率，首次使用时采用一倍速。
    public static void Resume(Map map)
    {
        CheckMap(map);
        SetRate(map, map.GetComponent<MapComponent_NingshaTime>().ResumeRate);
    }

    //包含已经申请接管、等待下一次世界 Tick 的地图。
    public static bool IsEnabled(Map map) => map?.GetComponent<MapComponent_NingshaTime>().Requested == true;

    //未接管地图的一倍速始终跟随世界。
    public static float GetRate(Map map) => IsEnabled(map) ? map.GetComponent<MapComponent_NingshaTime>().Rate : 1f;

    //读取地图逻辑时间；传入 null 时读取世界时间。
    public static int GetTicks(Map map)
    {
        var clock = ActiveClock(map);
        return clock != null ? clock.Ticks : MapTimeScope.WorldTicks;
    }

    //绝对时间沿用游戏开局日期，地图间只改变已经流逝的 Tick。
    public static int GetTicksAbs(Map map) => GetTicks(map) + Find.TickManager.gameStartAbsTick;

    //让地图外触发的技能、生成与查询使用指定地图的时间，异常时也会恢复上下文。
    public static void RunInMapContext(Map map, Action action)
    {
        CheckMap(map);
        ArgumentNullException.ThrowIfNull(action);
        using var scope = new MapTimeScope(map);
        action();
    }

    //读取实际已经启用的地图时钟，避免在 Tick 中途改变队列归属。
    internal static MapComponent_NingshaTime ActiveClock(Map map)
    {
        var clock = map?.GetComponent<MapComponent_NingshaTime>();
        return clock?.Enabled == true ? clock : null;
    }

    //交给调用者适配自身组件，异常正常向上传播。
    internal static void NotifyTimeShifted(Thing thing, int delta) => ThingTimeShifted?.Invoke(thing, delta);

    //限制外部写入入口，禁止后台线程切换原版全局时间。
    private static void CheckMap(Map map)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (!UnityData.IsInMainThread)
            throw new InvalidOperationException("凝砂异步时序 API 必须在游戏主线程调用。");
        if (map.Disposed || !Find.Maps.Contains(map))
            throw new ArgumentException("地图必须仍在当前游戏中。", nameof(map));
    }
}
