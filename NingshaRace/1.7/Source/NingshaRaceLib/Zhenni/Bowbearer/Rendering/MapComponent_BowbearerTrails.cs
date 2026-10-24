using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//尾迹独立于弹丸寿命，命中后自然收细消散。
public sealed class MapComponent_BowbearerTrails : MapComponent
{
    private readonly List<BowbearerSpiralTrail> trails = new();

    //每张地图持有自己的尾迹集合。
    public MapComponent_BowbearerTrails(Map map) : base(map) { }

    //弹丸只在首次创建尾迹时登记一次。
    internal void Add(BowbearerSpiralTrail trail) { trails.Add(trail); }

    //清理过期尾迹，不在绘制期间推进游戏状态。
    public override void MapComponentTick()
    {
        float time = Find.TickManager.TicksGame / 60f;
        for (int i = trails.Count - 1; i >= 0; i--)
            if (trails[i].Expired(time)) { trails[i].Dispose(); trails.RemoveAt(i); }
    }

    //只提交当前地图的特效，其他地图仍按游戏时间正常过期。
    public override void MapComponentDraw()
    {
        if (map != Find.CurrentMap) return;
        float time = Find.TickManager.TicksGame / 60f;
        foreach (var trail in trails) trail.Draw(time, false, map);
    }

    //地图被移除时释放仍未淡出的动态网格。
    public override void MapRemoved()
    {
        foreach (var trail in trails) trail.Dispose();
        trails.Clear();
    }
}
