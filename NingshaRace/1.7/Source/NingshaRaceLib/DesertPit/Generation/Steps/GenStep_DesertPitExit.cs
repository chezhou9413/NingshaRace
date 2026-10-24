using System;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Generation.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Steps;

//使用基座算法选定的干地入口，保持周围洞壁与河岸不变。
public class GenStep_DesertPitExit : GenStep
{
    //出口周围保留一小片沙地，供后续生态铺地共同避让。
    internal const float SandRadius = 3f;

    public override int SeedPart => 914027333;

    //设置玩家进入点，并为口袋地图绑定返回地表的出口。
    public override void Generate(Map map, GenStepParams parms)
    {
        DesertPitGenUtility.SetGenerationStatus("洞穴出口");
        var data = DesertPitGenUtility.GetLayoutData();
        IntVec3 exitCell = data.MainCenter;
        if (!exitCell.InBounds(map) || !exitCell.Standable(map) || exitCell.GetTerrain(map).IsWater)
            throw new InvalidOperationException("洞穴基座入口不是可站立干地。");
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(exitCell, SandRadius, true))
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetTerrain(map).IsWater) continue;
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Sand);
            data.ProtectedRouteCells.Add(cell);
        }
        MapPortal portal = PocketMapUtility.currentlyGeneratingPortal;
        if (portal != null)
        {
            var exit = (PocketMapExit)GenSpawn.Spawn(ThingMaker.MakeThing(DefOfRefs.NingshaRace_DesertPitCaveExit), exitCell, map);
            portal.exit = exit;
        }
        MapGenerator.PlayerStartSpot = exitCell;
    }
}
