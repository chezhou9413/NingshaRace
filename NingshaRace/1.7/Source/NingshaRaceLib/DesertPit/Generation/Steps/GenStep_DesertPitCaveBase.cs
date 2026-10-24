using System;
using System.Collections;
using System.Globalization;
using NingshaRaceLib.DesertPit.Generation.CaveBase;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.DesertPit.Generation.Regions;
using NingshaRaceLib.DesertPit.Generation.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Steps;

//把实验台 v14 的纯地形阶段接入实际地图；不执行生态、遗迹或资源填充。
public sealed class GenStep_DesertPitCaveBase : GenStep, IDesertPitIncrementalGenStep
{
    public override int SeedPart => 914027331;

    //原版同步入口与增量入口执行同一算法。
    public override void Generate(Map map, GenStepParams parms)
    {
        foreach (object unused in GenerateIncrementally(map, parms)) { }
    }

    //阶段之间交还主线程，随机源仅在入口读取一次。
    public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
    {
        if (map.Size.x != map.Size.z) throw new InvalidOperationException("v14 洞穴基座需要正方形地图。");
        string seed = Rand.Int.ToString(CultureInfo.InvariantCulture);
        string template = new[] { "lake", "confluence", "braided" }[(int)(CaveMath.Random(seed + ":template")() * 3)];
        var settings = new CaveSettings(seed, map.Size.x, template);
        Log.Message($"[凝砂洞穴] v14 基座：地图 {map.uniqueID}，模板 {template}，种子 {seed}，尺寸 {settings.Size}。");
        DesertPitGenUtility.SetGenerationStatus("洞穴基座：洞室与河湖");
        var plan = CaveTemplates.Create(settings);
        CaveHydrology.Build(plan, settings);
        DesertPitGenerationProgress.SetStepFraction(.08f);
        yield return null;
        CaveConnections.AddRooms(plan, settings);
        yield return null;
        CaveConnections.Connect(plan, settings);
        DesertPitGenerationProgress.SetStepFraction(.2f);
        yield return null;

        var fields = new CaveFields(settings);
        var lakeRandom = CaveMath.Random(seed + ":lake-shapes");
        foreach (var lake in plan.Lakes) fields.Blob(fields.Water, lake, lakeRandom, 3);
        uint bankSeed = CaveMath.Hash(seed + ":lake-banks");
        int n = settings.Size;
        for (int i = 0; i < fields.Cave.Length; i++)
        {
            double bank = settings.Banks / n * (1 + .65 * CaveMath.Noise(i % n / (double)n * 19, i / n / (double)n * 19, bankSeed));
            fields.Cave[i] = (float)Math.Min(fields.Cave[i], fields.Water[i] - Math.Max(1.5 / n, bank));
        }
        foreach (var river in plan.Rivers)
        {
            fields.Stroke(fields.Water, river, fields.WaterCore, 1.05 / n);
            fields.Stroke(fields.Cave, river, null, 0, true);
            yield return null;
        }
        var caveRandom = CaveMath.Random(seed + ":cave-shapes");
        foreach (var room in plan.Rooms)
        {
            fields.Blob(fields.Cave, room, caveRandom, room.Main ? 4 : 2);
            fields.Disk(fields.Protected, room, 2.0 / n);
        }
        foreach (var tunnel in plan.Tunnels) fields.Stroke(fields.Cave, tunnel, fields.Protected, 1.05 / n);
        foreach (var pool in plan.Pools) fields.Pond(pool);
        fields.AddPillars(plan, settings);
        byte[] cells = CaveRaster.Classify(fields);
        int start = CaveRaster.ChooseStart(fields, cells, plan.Spawn);
        plan.Spawn = new CavePlan.Point((start % n + .5) / n, (start / n + .5) / n);
        DesertPitGenerationProgress.SetStepFraction(.4f);
        DesertPitGenUtility.SetGenerationStatus("洞穴基座：末端空洞");
        yield return null;
        foreach (object unused in CaveEndRooms.Carve(cells, start, plan, settings)) yield return null;

        var data = DesertPitGenUtility.GetLayoutData();
        data.MainCenter = new IntVec3(start % n, 0, start / n);
        data.Ecology = CaveEcologyLayout.Build(cells, start, plan, settings);
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = new IntVec3(i % n, 0, i / n);
            if (fields.Protected[i] != 0) data.ProtectedRouteCells.Add(cell);
            if (data.Ecology.Reserved[i] != 0) data.ReservedSceneCells.Add(cell);
        }
        foreach (var room in plan.EndRooms)
            foreach (var point in room.Connector)
            {
                var center = new IntVec3((int)(point.X * n), 0, (int)(point.Y * n));
                if (center.DistanceTo(data.Ecology.Cell(room.Center)) < 6f) continue;
                foreach (var cell in GenRadial.RadialCellsAround(center, 1.5f, true))
                    if (cell.InBounds(map)) data.ProtectedRouteCells.Add(cell);
            }
        MapGenerator.PlayerStartSpot = data.MainCenter;
        MapGenerator.Caves.Clear();
        TerrainDef rough = DefDatabase<TerrainDef>.GetNamed("Sandstone_Rough");
        DesertPitGenUtility.SetGenerationStatus("洞穴基座：岩体与地面");
        using (map.pathing.DisableIncrementalScope())
        {
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = new IntVec3(i % n, 0, i / n);
                map.roofGrid.SetRoof(cell, map.generatorDef.roofDef);
                MapGenerator.Caves[cell] = cells[i] == 0 ? 0f : 1f;
                TerrainDef terrain = cells[i] switch
                {
                    2 => TerrainDefOf.WaterShallow,
                    3 => TerrainDefOf.WaterDeep,
                    1 when fields.Cave[i] > -.006 => rough,
                    _ => TerrainDefOf.Sand
                };
                map.terrainGrid.SetTerrain(cell, terrain);
                if (cells[i] == 0) GenSpawn.Spawn(ThingDefOf.Sandstone, cell, map);
                if ((i + 1) % 768 == 0)
                {
                    DesertPitGenerationProgress.SetStepFraction(.5f + .5f * (i + 1) / cells.Length);
                    yield return null;
                }
            }
        }
        DesertPitGenerationProgress.SetStepFraction(1f);
    }
}
