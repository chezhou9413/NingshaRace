using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Generation;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Lighting;
using NingshaRaceLib.DesertPit.Generation.Regions;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Generation;

//按洞室与明暗边界配置巢群、菌床、天光和自然装饰。
internal static class DesertPitRegionalBuildings
{
    //先放巢群及菌床，后续植物和能源点共同避让其占地。
    internal static void Habitats(Map map, DesertPitLayoutData data)
    {
        var layout = data.Ecology;
        for (int i = 0; i < layout.Plan.EndRooms.Count; i++)
        {
            var room = layout.Plan.EndRooms[i];
            var center = DesertPitAntSceneUtility.GenerateInNaturalChamber(map, data, room, i);
        }
        AntHabitatGeneration.SpawnRepellent(map, data, data.MainCenter, 3f, 5f);
    }

    //天光只落在既定亮区；晶体、钟乳石和落沙按不同岸带稀疏散布。
    internal static void Landmarks(Map map, DesertPitLayoutData data)
    {
        var layout = data.Ecology;
        var order = Enumerable.Range(0, layout.Cells.Length).Where(i => layout.RegionAt(i) != 0).ToList(); order.Shuffle();
        var lights = new List<IntVec3>(); var decorations = new List<IntVec3>();
        var glow = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlow");
        foreach (int i in order)
        {
            var cell = layout.Cell(i);
            if (layout.Light[i] != 2 || lights.Any(c => c.DistanceTo(cell) < 8f) || !Clear(map, data, cell, glow, false)) continue;
            GenSpawn.Spawn(glow, cell, map); lights.Add(cell);
        }
        foreach (int i in order)
        {
            var cell = layout.Cell(i);
            if (layout.Oasis[i] != 0 || cell.DistanceTo(data.MainCenter) < 12f || decorations.Any(c => c.DistanceTo(cell) < 4f)) continue;
            string suffix;
            if (layout.Light[i] is 1 or 2)
            {
                if (!Rand.Chance(.015f)) continue;
                suffix = Rand.Bool ? "GlowCrystalShard" : "GlowCrystal";
            }
            else
            {
                if (!Rand.Chance(layout.WallDistance[i] <= 3 ? .035f : .008f)) continue;
                suffix = Rand.Chance(.12f) ? "CeilingSandfall" : "Stalactite" + (char)('A' + Rand.Range(0, 6));
            }
            var def = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPit" + suffix);
            if (!Clear(map, data, cell, def, true)) continue;
            GenSpawn.Spawn(def, cell, map); decorations.Add(cell);
            foreach (var occupied in GenAdj.OccupiedRect(cell, Rot4.North, def.size)) data.ReservedSceneCells.Add(occupied);
        }
        DesertPitGenerationLighting.Refresh(map);
    }

    //检查完整占地，生成时绝不依靠覆盖已有实体清场。
    private static bool Clear(Map map, DesertPitLayoutData data, IntVec3 center, ThingDef def, bool keepRoutes)
    {
        foreach (var cell in GenAdj.OccupiedRect(center, Rot4.North, def.size))
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetTerrain(map).IsWater || cell.DistanceTo(data.MainCenter) < 2f
                || data.ReservedSceneCells.Contains(cell) || keepRoutes && data.ProtectedRouteCells.Contains(cell)) return false;
            foreach (var thing in cell.GetThingList(map))
                if (thing is Building || thing is Plant || thing is Pawn || thing.def.category == ThingCategory.Item) return false;
        }
        return true;
    }
}
