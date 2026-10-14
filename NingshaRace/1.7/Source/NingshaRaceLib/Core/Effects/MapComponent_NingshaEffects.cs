using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Core.Effects;

//地图持有一次性 RimUnity 实例，寿命跟随游戏 Tick。
public sealed class MapComponent_NingshaEffects : MapComponent
{
    private readonly List<(RimUnity.GameObject Instance, int Expires)> instances = new();

    //视觉实例不进入存档。
    public MapComponent_NingshaEffects(Map map) : base(map) { }

    //离屏地图立即隐藏，新实例不会在当前地图闪现。
    internal void Track(RimUnity.GameObject instance, float seconds)
    {
        instance.SetActive(Find.CurrentMap == map && !RimWorld.Planet.WorldRendererUtility.WorldRendered);
        instances.Add((instance, Find.TickManager.TicksGame + UnityEngine.Mathf.CeilToInt(seconds * 60)));
    }

    //切换地图时同步显隐，避免坐标相同的地图共用特效画面。
    public override void MapComponentUpdate()
    {
        bool visible = Find.CurrentMap == map && !RimWorld.Planet.WorldRendererUtility.WorldRendered;
        foreach (var item in instances) if (item.Instance != null) item.Instance.SetActive(visible);
    }

    //到达技能设定寿命后释放对象和实例材质。
    public override void MapComponentTick()
    {
        for (int i = instances.Count - 1; i >= 0; i--)
            if (instances[i].Instance == null || Find.TickManager.TicksGame >= instances[i].Expires)
            {
                RimUnity.Object.Destroy(instances[i].Instance);
                instances.RemoveAt(i);
            }
    }

    //地图移除时立即停止，场景退出另由 RimUnity 统一清理。
    public override void MapRemoved()
    {
        foreach (var item in instances) RimUnity.Object.Destroy(item.Instance);
        instances.Clear();
    }
}
