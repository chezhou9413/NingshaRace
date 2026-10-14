using System.Collections.Generic;
using NingshaRaceLib.Core.Effects;
using UnityEngine;
using Verse;
using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Effects;

//每个侵蚀体只持有一个烟雾句柄，地图卸载时同步释放。
public sealed class MapComponent_ErosionParticles : MapComponent
{
    private readonly Dictionary<Pawn, RimUnity.GameObject> handles = new();
    private readonly List<Pawn> expired = new();

    //视觉缓存不写入存档。
    public MapComponent_ErosionParticles(Map map) : base(map) { }

    //粒子的局部变换已经烘入配方，仅叠加头部姿态和百分之十五的整体缩放。
    public void UpdateHead(Pawn pawn, Vector3 position, Quaternion rotation)
    {
        if (!handles.TryGetValue(pawn, out var handle) || handle == null)
        {
            handle = NingshaParticleUtility.Create("NingshaRace_QinshiYanwu", position, rotation, .15f);
            handles[pawn] = handle;
        }
        handle.transform.SetPositionAndRotation(new(position.x, position.y, position.z), new(rotation.x, rotation.y, rotation.z, rotation.w));
    }

    //脱离侵蚀状态、死亡或离图时立即停止，避免字典保留失效 Pawn。
    public override void MapComponentTick()
    {
        foreach (var pair in handles)
            if (pair.Value == null || pair.Key.Destroyed || pair.Key.Dead || !pair.Key.Spawned ||
                pair.Key.Map != map || !ErosionPawnUtility.IsErosionBody(pair.Key))
            { RimUnity.Object.Destroy(pair.Value); expired.Add(pair.Key); }
        foreach (Pawn pawn in expired) handles.Remove(pawn);
        expired.Clear();
    }

    //只有当前地图的可见 Pawn 保持激活，避免地图切换或战争迷雾泄露粒子。
    public override void MapComponentUpdate()
    {
        bool visible = Find.CurrentMap == map && !RimWorld.Planet.WorldRendererUtility.WorldRendered;
        foreach (var pair in handles)
            if (pair.Value != null) pair.Value.SetActive(visible && pair.Key.Spawned && !pair.Key.Position.Fogged(map));
    }

    //地图移除时释放所有实例。
    public override void MapRemoved()
    {
        foreach (var handle in handles.Values) RimUnity.Object.Destroy(handle);
        handles.Clear();
        expired.Clear();
    }
}
