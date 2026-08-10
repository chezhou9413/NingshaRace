using System.Collections;
using System.Collections.Generic;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Progress;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.DesertPit.AntColony.Generation
{
    //类职责：在水文完成后按分帧生成协议向沙漠巨坑放置一至两个独立蚁巢场景。
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

        //函数职责：生成首个必有巢群，并在概率命中且有合法位置时生成第二个巢群。
        public IEnumerable GenerateIncrementally(Map map, GenStepParams parms)
        {
            DesertPitGenUtility.SetGenerationStatus("蚁巢生态");
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            DefModExtension_AntColony settings = DefOfRefs.NingshaRace_DesertPitAntNest.GetModExtension<DefModExtension_AntColony>();
            List<IntVec3> centers = new List<IntVec3>();
            IntVec3 center;
            if (!DesertPitAntSceneUtility.TryGenerateColony(map, data, centers, out center))
            {
                throw new System.InvalidOperationException("沙漠巨坑没有找到可生成首个蚁巢场景的位置。");
            }

            centers.Add(center);
            yield return null;

            if (Rand.Chance(settings.secondColonyChance) && DesertPitAntSceneUtility.TryGenerateColony(map, data, centers, out center))
            {
                centers.Add(center);
                yield return null;
            }
        }
    }
}
