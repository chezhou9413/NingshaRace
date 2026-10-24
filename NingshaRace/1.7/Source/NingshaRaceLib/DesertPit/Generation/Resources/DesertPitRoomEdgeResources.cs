using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Topology;
using NingshaRaceLib.DesertPit.Generation.Utility;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Resources
{
    //类职责：按主洞与次要洞室实际砂岩洞壁六格内的面积，补充钟乳石和发光晶体。
    internal static class DesertPitRoomEdgeResources
    {
        //函数职责：逐个洞室去重边缘面积，在已有植物和地貌之间按空位落实局部分区资源目标。
        public static void Generate(Map map, DesertPitLayoutData data)
        {
            if (data.Rooms.Count == 0) return;
            List<DesertPitRoom> rooms = new List<DesertPitRoom> { data.Rooms[0] };
            rooms.AddRange(data.SecondaryRooms);
            HashSet<IntVec3> counted = new HashSet<IntVec3>();
            foreach (var room in rooms)
            {
                HashSet<IntVec3> edges = new HashSet<IntVec3>();
                foreach (IntVec3 cell in room.Floor)
                {
                    if (counted.Contains(cell) || data.ReservedSceneCells.Contains(cell) || data.RiverCorridorCells.Contains(cell)
                        || DesertPitGenUtility.IsWaterLikeTerrain(cell.GetTerrain(map))) continue;
                    //以真实砂岩洞壁为边界，不把洞室之间已经敞开的相交轮廓当作墙壁。
                    foreach (IntVec3 near in GenRadial.RadialCellsAround(cell, 6f, true))
                    {
                        if (!near.InBounds(map) || MapGenerator.Caves[near] > 0f) continue;
                        Thing rock = near.GetEdifice(map);
                        if (rock?.def.building?.isNaturalRock == true) { edges.Add(cell); break; }
                    }
                }
                counted.UnionWith(edges);
                List<List<IntVec3>> regions = DesertPitResourceRegions.AlongRoom(edges, room.Center);
                for (int region = 0; region < regions.Count; region++)
                {
                    int stones = Rand.RangeInclusive(10, 18);
                    int crystals = Rand.RangeInclusive(2, 3);
                    //通行核心参与面积统计，资源只能落在分区内的非保护格。
                    regions[region].RemoveAll(data.ProtectedRouteCells.Contains);
                    string label = "洞室" + room.Center + "边缘分区" + (region + 1);
                    List<IntVec3> chosen = DesertPitResourcePlacement.TakeAvailable(map, regions[region], stones + crystals, label);
                    //空位不足时按目标比例缩减两类资源，把取整余量留给数量较少的晶体。
                    int actualStones = stones * chosen.Count / (stones + crystals);
                    for (int i = 0; i < chosen.Count; i++)
                        GenSpawn.Spawn(i < actualStones ? DesertPitDecorationUtility.ChooseStalactiteDef() : DesertPitDecorationUtility.ChooseCrystalDef(), chosen[i], map);
                }
            }
        }
    }
}
