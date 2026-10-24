using System;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.Data;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Components;

//保存群落边界与通道净空，使自然补生继续遵守最初分区。
public sealed class MapComponent_DesertPitRegions : MapComponent
{
    private byte[] regions;
    public MapComponent_DesertPitRegions(Map map) : base(map) { }
    internal bool Active => regions != null;

    //低四位保存群落，高位记录不能长树的狭窄地面。
    internal void Initialize(DesertPitLayoutData data)
    {
        var layout = data.Ecology;
        regions = new byte[layout.Cells.Length];
        for (int i = 0; i < regions.Length; i++)
        {
            var cell = layout.Cell(i);
            if (data.ProtectedRouteCells.Contains(cell) || data.ReservedSceneCells.Contains(cell) || cell.DistanceTo(data.MainCenter) < 6f) continue;
            if (cell.GetTerrain(map) == TerrainDefOf.Mud) continue;
            regions[i] = layout.RegionAt(i);
            if (regions[i] != 0 && (layout.WallDistance[i] < 3 || data.RiverCorridorCells.Contains(cell))) regions[i] |= 64;
        }
    }

    //只查询真实地图范围，水域与预留区返回零。
    internal byte RegionAt(IntVec3 cell) => Active && cell.InBounds(map) ? (byte)(regions[map.cellIndices.CellToIndex(cell)] & 15) : (byte)0;

    //初始种植和后续补生共享群落、实际光照及树木净空限制。
    internal bool Allows(ThingDef plant, IntVec3 cell)
    {
        if (!Active) return true;
        if (!cell.InBounds(map) || !DesertPitRegionalPlants.BelongsTo(plant, RegionAt(cell))) return false;
        if (plant.plant.diesToLight && map.glowGrid.GroundGlowAt(cell, true) > 0f) return false;
        if (map.glowGrid.GroundGlowAt(cell) < plant.plant.growMinGlow) return false;
        if (!plant.plant.IsTree) return true;
        if ((regions[map.cellIndices.CellToIndex(cell)] & 64) != 0) return false;
        foreach (var offset in GenAdj.AdjacentCells)
        {
            var adjacent = cell + offset;
            if (!adjacent.InBounds(map) || !adjacent.Standable(map) || adjacent.GetTerrain(map).IsWater || adjacent.GetEdifice(map) != null) return false;
        }
        foreach (var adjacent in GenRadial.RadialCellsAround(cell, 4f, false))
            if (adjacent.InBounds(map) && adjacent.GetPlant(map)?.def.plant.IsTree == true) return false;
        return true;
    }

    //紧凑保存每格一个字节，不把生成时的几何对象写入存档。
    public override void ExposeData()
    {
        base.ExposeData();
        string encoded = Scribe.mode == LoadSaveMode.Saving && Active ? Convert.ToBase64String(regions) : null;
        Scribe_Values.Look(ref encoded, "desertPitRegions");
        if (Scribe.mode == LoadSaveMode.LoadingVars && encoded != null)
        {
            regions = Convert.FromBase64String(encoded);
            if (regions.Length != map.Size.x * map.Size.z) throw new InvalidOperationException("凝砂地底生态分区尺寸与地图不一致。");
        }
    }
}
