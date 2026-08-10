using System.Collections;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.Generation.Steps
{
    //类职责：调度沙漠巨坑 V2 洞穴生成流程，并记录塌方地貌点。
    public class GenStep_DesertPitLayout : GenStep, IDesertPitIncrementalGenStep
    {
        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => 914027331;

        //函数职责：生成主洞室、支洞、小洞室、虫道、回环、边缘侵蚀和塌方记录。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：在洞室、通道、侵蚀和缓存阶段之间交还主线程帧，保持进度窗口响应。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("洞穴轮廓");
            MapGenerator.Caves.Clear();
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            data.SmallRooms.Clear();
            data.Collapses.Clear();
            data.ProtectedRouteCells.Clear();
            data.CaveEdgeDistances = null;

            List<DesertPitCaveNode> nodes = DesertPitCaveGraphUtility.BuildCaveGraph(map, data);
            DesertPitCaveCarver.CarveRooms(map, nodes);
            DesertPitGenerationProgress.SetStepFraction(0.2f);
            yield return null;
            DesertPitCaveCarver.CarveGraphTunnels(map, data, nodes);
            DesertPitGenerationProgress.SetStepFraction(0.4f);
            yield return null;
            DesertPitCaveCarver.RunEdgeErosion(map, data, nodes);
            DesertPitGenerationProgress.SetStepFraction(0.65f);
            yield return null;
            DesertPitCaveCarver.EnsureGraphConnected(map, data, nodes);
            DesertPitCaveCarver.CarveBlindPockets(map, data, nodes);
            DesertPitGenerationProgress.SetStepFraction(0.82f);
            yield return null;
            DesertPitGenUtility.ClearSafeArea(map, data.MainCenter, 6f);
            DesertPitGenUtility.BuildCaveEdgeCache(map);
            GenerateCollapses(map, data);
            DesertPitGenerationProgress.SetStepFraction(1f);
        }

        //函数职责：记录靠近洞壁和支洞末端的塌方区域，供地形步骤生成碎石。
        private static void GenerateCollapses(Map map, DesertPitLayoutData data)
        {
            int targetCount = Rand.RangeInclusive(5, 9);
            int added = 0;
            int attempts = 0;
            while (added < targetCount && attempts < targetCount * 14)
            {
                IntVec3 center = data.SmallRooms.Count > 0 ? data.SmallRooms.RandomElement() : data.MainCenter;
                IntVec3 collapse = center + new IntVec3(Rand.Range(-10, 11), 0, Rand.Range(-10, 11));
                if (DesertPitGenUtility.IsCave(map, collapse) && DesertPitGenUtility.NearCaveEdge(map, collapse, 4) && data.TryAddCollapse(collapse, 9f))
                {
                    added++;
                }

                attempts++;
            }
        }
    }
}
