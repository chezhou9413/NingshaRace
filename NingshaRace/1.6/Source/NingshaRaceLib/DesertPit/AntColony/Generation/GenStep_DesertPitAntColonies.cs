using System.Collections;
using System.Collections.Generic;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.Ecology.Generation;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.AntColony.Generation
{
    //类职责：在预留洞室安置两座巢群、三座菌巢和入口驱蚁桩。
    public class GenStep_DesertPitAntColonies : GenStep, IDesertPitIncrementalGenStep
    {
        private const int Seed = 914027346;

        public override int SeedPart => Seed;

        //函数职责：在原版同步地图生成入口中完整执行蚁巢场景迭代器。
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (object unused in GenerateIncrementally(map, parms))
            {
            }
        }

        //函数职责：逐洞室生成巢群和配对菌巢，再生成独立菌巢与入口保护物件。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("蚁巢生态");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            if (data.AntChambers.Count != 2) throw new System.InvalidOperationException("沙漠巨坑缺少预先规划的两座蚁巢洞室。");
            for (int i = 0; i < data.AntChambers.Count; i++)
            {
                var room = data.AntChambers[i];
                DesertPitAntSceneUtility.GenerateInChamber(map, data, room, i);
                AntHabitatGeneration.SpawnMound(map, data, room.Mound);
                yield return null;
            }
            IntVec3 freeMound = AntHabitatGeneration.FindMoundCell(map, data, data.MainCenter, 18f, 35f, false);
            AntHabitatGeneration.SpawnMound(map, data, freeMound);
            AntHabitatGeneration.SpawnRepellent(map, data, data.MainCenter, 3f, 5f);
        }
    }
}
