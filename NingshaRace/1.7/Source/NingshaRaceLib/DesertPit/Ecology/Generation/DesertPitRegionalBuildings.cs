using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Generation;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Lighting;
using NingshaRaceLib.DesertPit.Generation.Regions;
using NingshaRaceLib.DesertPit.Generation.Utility;
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

    //天光只落在既定亮区，河岸生成石簇，其余晶体与落沙按群落稀疏散布。
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
        ShoreStalactites(map, data, order, decorations);
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

    //沿实际岸带形成疏密不均的小石簇，明暗河岸均可出现，并为其他景观保留间隔。
    private static void ShoreStalactites(Map map, DesertPitLayoutData data, List<int> order, List<IntVec3> decorations)
    {
        var layout = data.Ecology;
        uint seed = CaveMath.Hash(layout.Settings.Seed + ":shore-stalactites");
        var centers = new List<IntVec3>();
        foreach (int i in order)
        {
            IntVec3 center = layout.Cell(i);
            if (!data.RiverCorridorCells.Contains(center) || center.DistanceToSquared(data.MainCenter) < 144
                || centers.Any(cell => cell.DistanceToSquared(center) < 100)) continue;
            float chance = (float)(.045 + .035 * CaveMath.Noise(center.x * .07, center.z * .07, seed));
            if (!Rand.Chance(chance)) continue;

            var candidates = GenRadial.RadialCellsAround(center, 3.5f, true)
                .Where(cell => data.RiverCorridorCells.Contains(cell) && cell.DistanceToSquared(data.MainCenter) >= 144).ToList();
            candidates.Shuffle();
            var placements = new List<(IntVec3 Cell, ThingDef Def)>();
            int target = Rand.RangeInclusive(3, 7);
            foreach (IntVec3 cell in candidates)
            {
                ThingDef def = DesertPitDecorationUtility.ChooseStalactiteDef();
                if (!Clear(map, data, cell, def, true)) continue;
                //大型残柱周围多留半格，细石允许相邻，避免整簇排成等距网格。
                if (placements.Any(other => cell.DistanceToSquared(other.Cell) <
                    (DesertPitDecorationUtility.IsLargeDecoration(def)
                        || DesertPitDecorationUtility.IsLargeDecoration(other.Def) ? 2.25f : 1f))) continue;
                placements.Add((cell, def));
                if (placements.Count == target) break;
            }

            //岸带太窄或被占用时整簇略过，不在水面或保护通道上补足数量。
            if (placements.Count < 3) continue;
            centers.Add(center);
            foreach (var placement in placements)
            {
                GenSpawn.Spawn(placement.Def, placement.Cell, map);
                decorations.Add(placement.Cell);
                foreach (IntVec3 occupied in GenAdj.OccupiedRect(placement.Cell, Rot4.North, placement.Def.size))
                    data.ReservedSceneCells.Add(occupied);
            }
        }
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
