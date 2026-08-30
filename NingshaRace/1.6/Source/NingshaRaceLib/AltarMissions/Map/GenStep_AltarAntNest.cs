using System;
using System.Collections.Generic;
using Verse;

using NingshaRaceLib.DesertPit.AntColony.Generation;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Utility;

namespace NingshaRaceLib.AltarMissions.Map
{
    //类职责：在清剿任务地下图生成恰好一个关闭营养升级的四级或五级蚁巢。
    public sealed class GenStep_AltarAntNest : GenStep
    {
        //属性职责：提供地图生成器使用的稳定随机种子片段。
        public override int SeedPart => 812740113;

        //函数职责：以各百分之五十概率选择固定等级并复用正式蚁巢场景生成器。
        public override void Generate(Verse.Map map, GenStepParams parms)
        {
            DesertPitLayoutData data = DesertPitGenUtility.GetLayoutData();
            int level = Rand.Chance(0.5f) ? 4 : 5;
            if (!DesertPitAntSceneUtility.TryGenerateColony(map, data, new List<IntVec3>(), level, false, out IntVec3 _))
            {
                throw new InvalidOperationException("清剿蚁巢任务没有找到固定蚁巢生成位置。");
            }
        }
    }
}
