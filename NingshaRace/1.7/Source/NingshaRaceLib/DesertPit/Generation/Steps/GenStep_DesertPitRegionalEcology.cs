using System;
using System.Collections;
using System.Linq;
using NingshaRaceLib.DesertPit.Ecology.Components;
using NingshaRaceLib.DesertPit.Ecology.Generation;
using NingshaRaceLib.DesertPit.Ecology.Utility;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Hydrology;
using NingshaRaceLib.DesertPit.Generation.Lighting;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.DesertPit.Generation.Resources;
using NingshaRaceLib.DesertPit.Generation.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Steps;

//在 v14 地形与区域完成后依次铺地、放建筑和种植，不重新雕刻洞穴。
public sealed class GenStep_DesertPitRegionalEcology : GenStep, IDesertPitIncrementalGenStep
{
    public override int SeedPart => 914027351;

    //同步与增量生成共用同一流程。
    public override void Generate(Map map, GenStepParams parms)
    {
        foreach (object unused in GenerateIncrementally(map, parms)) { }
    }

    //依赖真实光照的植物最后落地，原版畏光菌只进入没有非菌类光源的位置。
    public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
    {
        var data = DesertPitGenUtility.GetLayoutData();
        if (data.Ecology == null) throw new InvalidOperationException("分区生态生成需要先完成 v14 洞穴基座。");
        DesertPitGenUtility.SetGenerationStatus("地底生态：土壤与菌床");
        PaintGround(map, data);
        DesertPitShoreTerrain.Paint(map, data);
        DesertPitGenerationProgress.SetStepFraction(.15f);
        yield return null;
        DesertPitRegionalBuildings.Habitats(map, data);
        DesertPitGenerationProgress.SetStepFraction(.3f);
        yield return null;
        data.CaveEdgeDistances = data.Ecology.WallDistance.Select(d => (byte)Math.Min(d, 6)).ToArray();
        new GenStep_DesertPitEnergySites().Generate(map, parms);
        DesertPitGenUtility.SetGenerationStatus("地底生态：天光与岩洞景观");
        DesertPitRegionalBuildings.Landmarks(map, data);
        var regions = map.GetComponent<MapComponent_DesertPitRegions>(); regions.Initialize(data);
        DesertPitGenerationProgress.SetStepFraction(.5f);
        yield return null;
        DesertPitGenUtility.SetGenerationStatus("地底生态：分区植被");
        foreach (object unused in Plants(map, data, regions)) yield return null;
        DesertPitGenerationLighting.Refresh(map);
        var counts = Enumerable.Range(0, data.Ecology.Cells.Length).GroupBy(data.Ecology.RegionAt)
            .Where(g => g.Key != 0).Select(g => $"{g.Key}:{g.Count()}");
        Log.Message($"[凝砂洞穴] 分区生态完成，群落面积 {string.Join(", ", counts)}，植物 {map.listerThings.ThingsInGroup(ThingRequestGroup.Plant).Count} 株。");
        data.Ecology = null;
        DesertPitGenerationProgress.SetStepFraction(1f);
    }

    //只替换已有干地；绿洲腹地富土、岸带泥土，暗处以菌砾斑块承载原版洞穴菌。
    private static void PaintGround(Map map, DesertPitLayoutData data)
    {
        var layout = data.Ecology; uint seed = CaveMath.Hash(layout.Settings.Seed + ":ecology-ground");
        for (int i = 0; i < layout.Cells.Length; i++)
        {
            if (layout.Cells[i] != 1 || layout.Walking[i] < 0 || layout.Reserved[i] != 0) continue;
            var cell = layout.Cell(i);
            //保留出口步骤已经铺好的沙地，避免被绿洲土壤或菌砾覆盖。
            if (cell.DistanceToSquared(data.MainCenter) <= GenStep_DesertPitExit.SandRadius * GenStep_DesertPitExit.SandRadius) continue;
            double noise = CaveMath.Noise(cell.x * .075, cell.z * .075, seed);
            TerrainDef terrain = null;
            if (layout.Oasis[i] == 2) terrain = noise > .05 ? TerrainDefOf.SoilRich : TerrainDefOf.Soil;
            else if (layout.Oasis[i] == 3) terrain = TerrainDefOf.Soil;
            else if (layout.Oasis[i] == 1 && noise > -.35) terrain = TerrainDefOf.Soil;
            else if (layout.Light[i] is 1 or 2) terrain = TerrainDefOf.Soil;
            else if (noise > -.35) terrain = TerrainDefOf.FungalGravel;
            if (terrain != null) map.terrainGrid.SetTerrain(cell, terrain);
        }
    }

    //树木与巨菇先获得净空，小型植被随后按连续噪声形成不同密度的群落。
    private static IEnumerable Plants(Map map, DesertPitLayoutData data, MapComponent_DesertPitRegions regions)
    {
        var layout = data.Ecology;
        var order = Enumerable.Range(0, layout.Cells.Length).Where(i => regions.RegionAt(layout.Cell(i)) != 0).ToList(); order.Shuffle();
        uint seed = CaveMath.Hash(layout.Settings.Seed + ":vegetation"); int processed = 0;
        foreach (bool trees in new[] { true, false })
            foreach (int i in order)
            {
                var cell = layout.Cell(i); byte region = regions.RegionAt(cell);
                double patch = .85 + .65 * CaveMath.Noise(cell.x * .11, cell.z * .11, seed);
                float density = trees ? (region is 6 or 7 ? .025f : .012f) : DesertPitRegionalPlants.Density(region);
                if (data.RiverCorridorCells.Contains(cell)) density *= .35f;
                if (Rand.Chance((float)(density * patch)))
                {
                    var plant = DesertPitRegionalPlants.Choose(region, trees, def => regions.Allows(def, cell)
                        && DesertPitPlantEcologyUtility.CanPlacePlant(map, cell, def, false));
                    if (plant != null) DesertPitPlantEcologyUtility.SpawnPlant(map, plant, cell, new FloatRange(.55f, 1f));
                }
                if (++processed % 512 == 0)
                {
                    DesertPitGenerationProgress.SetStepFraction(.5f + .5f * processed / (order.Count * 2));
                    yield return null;
                }
            }
    }
}
