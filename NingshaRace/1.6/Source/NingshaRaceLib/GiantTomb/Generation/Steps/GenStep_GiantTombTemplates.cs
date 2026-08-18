using System;
using System.Collections;
using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.GiantTomb.Layout;
using NingshaRaceLib.PocketMaps.Generation;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation.Steps
{
    //类职责：实例化全部墓葬模板、清理捕获背景并规范相邻重复门。
    public sealed class GenStep_GiantTombTemplates : GenStep, INingshaIncrementalGenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 197428341;

        //函数职责：兼容原版同步生成入口并完整执行模板生成流程。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：逐实例生成模板并在每个完整模块完成后交还画面帧。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            GiantTombLayoutData data = GiantTombGenUtility.GetLayoutData();
            TerrainDef sandstone = DefDatabase<TerrainDef>.GetNamed("Sandstone_Rough");
            ClMapSpawnOptions options = new ClMapSpawnOptions
            {
                ApplyTerrain = true,
                ApplyTerrainColor = true,
                ApplyRoof = false,
                ApplyFog = false,
                ApplyPollution = false,
                SpawnThings = true,
                ConflictPolicy = ClMapConflictPolicy.Reject,
                FactionResolver = ResolveTemplateFaction
            };
            using (new GiantTombBulkMapUpdateScope(map))
            {
                for (int i = 0; i < data.Placements.Count; i++)
                {
                    GiantTombPlacement placement = data.Placements[i];
                    DesertPitGenerationProgress.SetStage("生成墓葬模块 " + (i + 1) + "/" + data.Placements.Count);
                    ClMapSpawnResult result = ClMapTemplateSpawner.Spawn(placement.Module.Template, map, placement.Origin, placement.Transform, options);
                    CleanupCapturedBackground(map, data, placement, result, sandstone);
                    DesertPitGenerationProgress.SetStepFraction((i + 1f) / data.Placements.Count);
                    yield return null;
                }
                RemoveDuplicateDoors(map, data.Connections);
            }
            DesertPitGenerationProgress.SetStepFraction(1f);
        }

        //函数职责：把模板中原属玩家的遗迹建筑改归敌对古代人阵营，并保留其他显式派系引用。
        private static Faction ResolveTemplateFaction(ClMapFactionReference reference)
        {
            if (reference.Mode == ClMapFactionMode.None)
            {
                return null;
            }
            if (reference.Mode == ClMapFactionMode.Player)
            {
                return Faction.OfAncientsHostile;
            }
            Faction faction = Find.FactionManager.FirstFactionOfDef(reference.Def);
            if (faction == null)
            {
                throw new InvalidOperationException("当前世界不存在墓葬模板所需派系: " + reference.Def.defName);
            }
            return faction;
        }

        //函数职责：移除模板矩形中结构掩码之外的捕获Thing并恢复统一砂岩地表。
        private static void CleanupCapturedBackground(Map map, GiantTombLayoutData data, GiantTombPlacement placement, ClMapSpawnResult spawnResult, TerrainDef sandstone)
        {
            for (int i = spawnResult.SpawnedThings.Count - 1; i >= 0; i--)
            {
                Thing thing = spawnResult.SpawnedThings[i];
                if (!thing.Spawned) continue;
                int inside = 0;
                int occupied = 0;
                foreach (IntVec3 cell in GenAdj.OccupiedRect(thing.Position, thing.Rotation, thing.def.Size))
                {
                    occupied++;
                    if (cell.InBounds(map) && data.StructureCells[map.cellIndices.CellToIndex(cell)]) inside++;
                }
                if (inside == 0)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
                else if (inside != occupied)
                {
                    throw new InvalidOperationException("墓葬模板Thing跨越结构边界: " + placement.Module.Def.defName + "/" + thing.def.defName);
                }
            }
            foreach (IntVec3 cell in placement.Bounds)
            {
                if (!data.StructureCells[map.cellIndices.CellToIndex(cell)])
                {
                    map.terrainGrid.SetTerrain(cell, sandstone);
                    map.terrainGrid.SetTerrainColor(cell, null);
                }
            }
        }

        //函数职责：在门对门连接中移除子模块一侧的重复门，避免形成两格连续门厅。
        private static void RemoveDuplicateDoors(Map map, List<GiantTombConnection> connections)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                GiantTombConnection connection = connections[i];
                if (connection.ParentConnector.Kind != GiantTombConnectorKind.Door || connection.ChildConnector.Kind != GiantTombConnectorKind.Door)
                {
                    continue;
                }
                for (int j = 0; j < connection.ChildConnector.Cells.Count; j++)
                {
                    IntVec3 cell = connection.ChildConnector.Cells[j];
                    Building_Door door = cell.GetEdifice(map) as Building_Door;
                    if (door == null)
                    {
                        throw new InvalidOperationException("门对门连接的子侧缺少门体: " + connection.Child.Module.Def.defName + " @ " + cell);
                    }
                    door.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
