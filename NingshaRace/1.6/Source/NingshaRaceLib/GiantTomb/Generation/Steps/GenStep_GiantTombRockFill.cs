using System.Collections;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.PocketMaps.Generation;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation.Steps
{
    //类职责：用天然砂岩实体填满所有不属于墓葬结构的地图格。
    public sealed class GenStep_GiantTombRockFill : GenStep, INingshaIncrementalGenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 197428357;

        //函数职责：兼容原版同步生成入口并完整执行天然岩层填充。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：分批在墓葬结构外生成可开采天然砂岩，并延迟提交区域与寻路更新。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            GiantTombLayoutData data = GiantTombGenUtility.GetLayoutData();
            DesertPitGenerationProgress.SetStage("填充墓葬外岩层");
            int processed = 0;
            int total = map.cellIndices.NumGridCells;
            using (new GiantTombBulkMapUpdateScope(map))
            {
                foreach (IntVec3 cell in map.AllCells)
                {
                    int index = map.cellIndices.CellToIndex(cell);
                    if (!data.StructureCells[index]) SpawnRockOnEmptyCell(map, cell);
                    processed++;
                    if (processed % 512 == 0)
                    {
                        DesertPitGenerationProgress.SetStepFraction((float)processed / total);
                        yield return null;
                    }
                }
            }
            DesertPitGenerationProgress.SetStepFraction(1f);
        }

        //函数职责：利用结构外必为空格的不变量直接登记单格天然岩石，跳过重复的可生成与擦除扫描。
        private static void SpawnRockOnEmptyCell(Map map, IntVec3 cell)
        {
            if (map.thingGrid.ThingsListAtFast(cell).Count != 0)
            {
                throw new System.InvalidOperationException("墓葬结构外岩层格存在未清理实体: " + cell);
            }
            Thing rock = ThingMaker.MakeThing(ThingDefOf.Sandstone);
            rock.Rotation = Rot4.North;
            rock.Position = cell;
            rock.SpawnSetup(map, false);
        }
    }
}
