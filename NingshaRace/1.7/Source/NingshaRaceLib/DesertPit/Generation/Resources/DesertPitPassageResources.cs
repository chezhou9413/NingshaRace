using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Topology;
using NingshaRaceLib.DesertPit.Generation.Utility;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Resources
{
    //类职责：按独立面积目标和局部空位，为实际通道及蚁巢连接口生成菌群与砂晶。
    internal static class DesertPitPassageResources
    {
        //函数职责：先生成蚁巢连接口菌群，再逐条通道划分独立目标，容量不足时只使用本区空位。
        public static void Generate(Map map, DesertPitLayoutData data)
        {
            ThingDef eggGrassDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitPlantE");
            ThingDef whiteSproutDef = DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitPlantB");
            HashSet<IntVec3> nestFootprints = DesertPitRoomBrush.AntFootprints(data);
            foreach (var nest in data.AntChambers)
            {
                List<IntVec3> area = new List<IntVec3>();
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(nest.ExternalAccessCell, 7f, true))
                    if (cell.InBounds(map) && !nestFootprints.Contains(cell) && !data.RiverCorridorCells.Contains(cell)
                        && (!data.ReservedSceneCells.Contains(cell) || data.PassageCells.Contains(cell))) area.Add(cell);
                int eggGrass = Rand.RangeInclusive(3, 5);
                int food = Rand.RangeInclusive(9, 13);
                List<IntVec3> cells = DesertPitResourcePlacement.TakeAvailable(map, area, eggGrass + food, "蚁巢连接口" + nest.ExternalAccessCell + "菌群");
                //按原始配额比例分配有限空位，至少两格时保留两类植物，单格优先较多的食用菌。
                int actualEggGrass = eggGrass * cells.Count / (eggGrass + food);
                if (cells.Count >= 2 && actualEggGrass == 0) actualEggGrass = 1;
                for (int i = 0; i < cells.Count; i++)
                    DesertPitResourcePlacement.Plant(map, cells[i], i < actualEggGrass ? eggGrassDef : DefOfRefs.NingshaRace_DesertPitPlantC);
            }

            HashSet<IntVec3> excluded = new HashSet<IntVec3>(nestFootprints);
            foreach (var room in data.Rooms) excluded.UnionWith(room.Floor);
            excluded.UnionWith(data.RiverCorridorCells);
            for (int passageIndex = 0; passageIndex < data.Passages.Count; passageIndex++)
            {
                DesertPitPassage passage = data.Passages[passageIndex];
                HashSet<IntVec3> area = new HashSet<IntVec3>(passage.Floor);
                area.ExceptWith(excluded);
                area.RemoveWhere(cell => !cell.InBounds(map) || !DesertPitGenUtility.IsCave(map, cell)
                    || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map)) || cell.DistanceTo(data.MainCenter) < 12f);
                //先登记面积归属，再检查当前实体占用，防止后续通道重复领取交叉区配额。
                excluded.UnionWith(area);
                List<List<IntVec3>> regions = DesertPitResourceRegions.AlongPassage(area, passage.Centerline);
                for (int region = 0; region < regions.Count; region++)
                {
                    int plants = Rand.RangeInclusive(3, 12);
                    string label = "通道" + (passageIndex + 1) + "分区" + (region + 1) + "，位置" + regions[region][0];
                    List<IntVec3> chosen = DesertPitResourcePlacement.TakeAvailable(map, regions[region], plants + 1, label);
                    //没有空位时继续下一分区；有空位时先保留一枚晶体，其余位置分配菌株。
                    if (chosen.Count == 0) continue;
                    GenSpawn.Spawn(DesertPitDecorationUtility.ChooseCrystalDef(), chosen[0], map);
                    for (int i = 1; i < chosen.Count; i++)
                        DesertPitResourcePlacement.Plant(map, chosen[i], Rand.Bool ? whiteSproutDef : DefOfRefs.NingshaRace_DesertPitPlantD);
                }
            }
        }
    }
}
