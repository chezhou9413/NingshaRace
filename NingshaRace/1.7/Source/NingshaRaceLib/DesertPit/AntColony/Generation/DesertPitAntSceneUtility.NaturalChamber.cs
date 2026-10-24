using System;
using System.Collections.Generic;
using System.Linq;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using NingshaRaceLib.DesertPit.Generation.Data;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Generation;

//将完整巢群安置在 v14 已开好的天然末端洞室中。
public static partial class DesertPitAntSceneUtility
{
    //只接受巢体及储藏环完整落在实际洞室里的位置，不改变洞壁和水系。
    internal static IntVec3 GenerateInNaturalChamber(Map map, DesertPitLayoutData data, CavePlan.EndRoom room, int index)
    {
        var layout = data.Ecology; var floor = new HashSet<IntVec3>(room.Floor.Select(layout.Cell));
        var origin = layout.Cell(room.Center); var nestDef = DefOfRefs.NingshaRace_DesertPitAntNest;
        int storageCount = nestDef.GetModExtension<DefModExtension_AntColony>().storageCellCount;
        foreach (var center in room.Floor.Select(layout.Cell).OrderBy(c => c.DistanceToSquared(origin)))
        {
            var occupied = GenAdj.OccupiedRect(center, Rot4.North, nestDef.size);
            if (occupied.ExpandedBy(2).Any(c => !floor.Contains(c))) continue;
            if (occupied.Any(c => !IsClearDryCaveCell(map, data, c, true))) continue;
            if (CollectStorageCells(map, data, occupied, true).Count < storageCount) continue;
            var entry = room.Connector[0];
            GenerateScene(map, data, nestDef, center, index, null, true, true,
                new IntVec3((int)(entry.X * layout.Size), 0, (int)(entry.Y * layout.Size)));
            return center;
        }
        throw new InvalidOperationException($"末端洞室 {index + 1} 无法容纳完整蚁巢及储藏环。");
    }
}
