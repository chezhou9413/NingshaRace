using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit
{
    //类职责：调度沙漠巨坑 V2 洞穴生成流程，并记录塌方地貌点。
    public class GenStep_DesertPitLayout : GenStep
    {
        //属性职责：提供当前生成步骤的稳定随机种子片段。
        public override int SeedPart => 914027331;

        //函数职责：生成主洞室、支洞、小洞室、虫道、回环、边缘侵蚀和塌方记录。
        public override void Generate(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("洞穴轮廓");
            MapGenerator.Caves.Clear();
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            data.SmallRooms.Clear();
            data.Collapses.Clear();

            List<DesertPitCaveNode> nodes = DesertPitCaveGraphUtility.BuildCaveGraph(map, data);
            DesertPitCaveCarver.CarveRooms(map, nodes);
            DesertPitCaveCarver.CarveGraphTunnels(map, nodes);
            DesertPitCaveCarver.RunEdgeErosion(map, nodes);
            DesertPitCaveCarver.EnsureGraphConnected(map, nodes);
            DesertPitCaveCarver.CarveBlindPockets(map, data, nodes);
            GenerateCollapses(map, data);
            DesertPitGenUtility.ClearSafeArea(map, data.MainCenter, 6f);
        }

        //函数职责：记录靠近洞壁和支洞末端的塌方区域，供地形步骤生成碎石。
        private static void GenerateCollapses(Map map, DesertPitLayoutData data)
        {
            int count = Rand.RangeInclusive(5, 9);
            for (int i = 0; i < count; i++)
            {
                IntVec3 center = data.SmallRooms.Count > 0 ? data.SmallRooms.RandomElement() : data.MainCenter;
                IntVec3 collapse = center + new IntVec3(Rand.Range(-10, 11), 0, Rand.Range(-10, 11));
                if (DesertPitGenUtility.IsCave(map, collapse))
                {
                    data.Collapses.Add(collapse);
                }
            }
        }
    }
}
