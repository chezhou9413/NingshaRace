using System.Collections;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.PocketMaps.Generation;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Generation.Steps
{
    //类职责：为巨型墓葬全图建立粗糙砂岩地面，厚岩顶由口袋地图通用流程统一铺设。
    public sealed class GenStep_GiantTombFoundation : GenStep, INingshaIncrementalGenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 197428327;

        //函数职责：兼容原版同步生成入口并完整执行基础层铺设。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：在地图首次初始化前直接填充粗糙砂岩底层数组，避免四万次地形通知。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            DesertPitGenerationProgress.SetStage("铺设墓葬岩层");
            TerrainDef sandstone = DefDatabase<TerrainDef>.GetNamed("Sandstone_Rough");
            PocketMapInitialGridUtility.FillUniformTerrain(map, sandstone);
            DesertPitGenerationProgress.SetStepFraction(1f);
            yield return null;
        }
    }
}
