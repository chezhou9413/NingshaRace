using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.AsyncTime;

//保存地图逻辑时间，并持有只由当地调度器推进的物体队列。
public sealed class MapComponent_NingshaTime : MapComponent
{
    private static readonly Action<PathFinder> CompletePaths = AccessTools.MethodDelegate<Action<PathFinder>>(
        AccessTools.Method(typeof(PathFinder), "ForceCompleteScheduledJobs"));
    private static readonly AccessTools.FieldRef<TimeSlower, int> ForcedUntil =
        AccessTools.FieldRefAccess<TimeSlower, int>("forceNormalSpeedUntil");
    private bool requested;
    private bool enabled;
    private bool ready;
    private int ticks;
    private float rate = 1f;
    private float resumeRate = 1f;
    private double remainder;
    private RimWorld.Storyteller storyteller;
    private TickList normal;
    private TickList rare;
    private TickList longTicks;

    internal readonly TimeSlower Slower = new();
    internal bool Requested => requested;
    internal bool Enabled => enabled;
    internal float Rate => rate;
    internal float ResumeRate => resumeRate;
    internal int Ticks => ticks;
    internal int FrameTicks;
    internal RimWorld.Storyteller Storyteller => storyteller;

    //不自动启用，保留原版地图的运行方式。
    public MapComponent_NingshaTime(Map map) : base(map) { }

    //设置请求状态，队列迁移只发生在世界 Tick 的安全边界。
    internal void SetRate(float value)
    {
        requested = true;
        rate = value;
        if (value > 0f) resumeRate = value;
        if (value == 0f) remainder = 0d;
    }

    //首次接管与载入后的队列重建都通过同一入口完成。
    internal void Prepare()
    {
        if (!requested || ready) return;
        normal ??= new TickList(TickerType.Normal);
        rare ??= new TickList(TickerType.Rare);
        longTicks ??= new TickList(TickerType.Long);
        if (!enabled)
        {
            ticks = MapTimeScope.WorldTicks;
            enabled = true;
            var original = Current.Game.storyteller;
            storyteller = new RimWorld.Storyteller(original.def, original.difficultyDef, original.difficulty);
            MapTimeIncidents.MoveQueuedIncidents(this);
        }
        Find.TickManager.RemoveAllFromMap(map);
        normal.Reset();
        rare.Reset();
        longTicks.Reset();
        foreach (Thing thing in map.listerThings.AllThings) Register(thing);
        MapTimeTransfers.RememberMap(map);
        ready = true;
    }

    //持有容器的物体始终进入普通队列，让内部物体沿用原版递归更新。
    private TickList ListFor(Thing thing) => thing is IThingHolder ? normal : thing.def.tickerType switch
    {
        TickerType.Normal => normal,
        TickerType.Rare => rare,
        TickerType.Long => longTicks,
        _ => null
    };

    //出生与跨图进入时注册根物体，内部物体不重复注册。
    internal void Register(Thing thing) => ListFor(thing)?.RegisterThing(thing);

    //离图时沿用原版延迟注销，避免在正在迭代的列表中移除元素。
    internal void Deregister(Thing thing) => ListFor(thing)?.DeregisterThing(thing);

    //世界每推进一 Tick，按倍率积累当地步数；暂停时不积压时间。
    internal void Advance()
    {
        if (!enabled || !ready || rate == 0f || map.Disposed) return;
        using var scope = new MapTimeScope(map);
        remainder += Slower.ForcedNormalSpeed ? Math.Min(rate, 1f) : rate;
        while (remainder >= 1d && rate > 0f && !map.Disposed)
        {
            remainder -= 1d;
            MapTimePatches.TickingMap = map;
            try
            {
                map.MapPreTick();
                //原版寻路作业读取地图快照，离开时钟上下文前必须完成。
                CompletePaths(map.pathFinder);
                MapTimeScope.SetTicks(++ticks);
                normal.Tick();
                rare.Tick();
                longTicks.Tick();
                MapTimeIncidents.Tick(this);
                map.MapPostTick();
                FrameTicks++;
            }
            finally
            {
                if (!map.Disposed) CompletePaths(map.pathFinder);
                MapTimePatches.TickingMap = null;
            }
        }
    }

    //只存储逻辑状态，TickList 在首次推进前从地图物体重建。
    public override void ExposeData()
    {
        Scribe_Values.Look(ref requested, "requested");
        Scribe_Values.Look(ref enabled, "enabled");
        Scribe_Values.Look(ref ticks, "localTicks");
        Scribe_Values.Look(ref rate, "rate", 1f);
        Scribe_Values.Look(ref resumeRate, "resumeRate", 1f);
        Scribe_Values.Look(ref remainder, "remainder");
        int forcedUntil = ForcedUntil(Slower);
        Scribe_Values.Look(ref forcedUntil, "forcedUntil");
        ForcedUntil(Slower) = forcedUntil;
        Scribe_Deep.Look(ref storyteller, "localStoryteller");
    }

    //载入后重建队列，暂停地图也能正确接收后续出生和注销。
    public override void FinalizeInit()
    {
        if (enabled) Prepare();
    }

    //释放队列对物体的引用。
    public override void MapRemoved()
    {
        normal?.Reset();
        rare?.Reset();
        longTicks?.Reset();
        ready = false;
    }
}
