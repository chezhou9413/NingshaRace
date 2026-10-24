using System;
using HarmonyLib;
using Verse;

namespace NingshaRaceLib.AsyncTime;

//在同一主线程中临时提供当地时钟；嵌套调用结束后恢复调用方。
internal sealed class MapTimeScope : IDisposable
{
    private static readonly AccessTools.FieldRef<TickManager, int> TicksField =
        AccessTools.FieldRefAccess<TickManager, int>("ticksGameInt");
    private static readonly AccessTools.FieldRef<TickManager, int> FrameTicksField =
        AccessTools.FieldRefAccess<TickManager, int>("ticksThisFrame");
    private static int depth;
    private static int worldTicks;
    private static TimeSlower worldSlower;
    private readonly TickManager manager;
    private readonly MapComponent_NingshaTime previousClock;
    private readonly int previousTicks;
    private readonly int previousFrameTicks;
    private readonly TimeSlower previousSlower;
    private bool disposed;

    internal static MapComponent_NingshaTime Current { get; private set; }
    internal static int WorldTicks => depth == 0 ? Find.TickManager.TicksGame : worldTicks;
    internal static bool InScope => depth != 0;

    //保存真实字段，避免修改游戏的暂停开关或当前可见地图。
    internal MapTimeScope(Map map)
    {
        manager = Find.TickManager;
        previousTicks = TicksField(manager);
        previousFrameTicks = FrameTicksField(manager);
        previousSlower = manager.slower;
        previousClock = Current;
        if (depth++ == 0)
        {
            worldTicks = previousTicks;
            worldSlower = previousSlower;
        }
        Current = NingshaTime.ActiveClock(map);
        TicksField(manager) = Current?.Ticks ?? worldTicks;
        manager.slower = Current?.Slower ?? worldSlower;
    }

    //推进一个当地 Tick 时同步原版计时入口。
    internal static void SetTicks(int ticks) => TicksField(Find.TickManager) = ticks;

    //地图渲染插值使用本地图实际完成的 Tick 数。
    internal static void SetFrameTicks(int ticks) => FrameTicksField(Find.TickManager) = ticks;

    //同时恢复时钟和临时状态，防止异常污染下一张地图。
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        TicksField(manager) = previousTicks;
        FrameTicksField(manager) = previousFrameTicks;
        manager.slower = previousSlower;
        Current = previousClock;
        depth--;
    }
}
