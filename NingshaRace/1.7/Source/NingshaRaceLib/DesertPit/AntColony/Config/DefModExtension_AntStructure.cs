using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Config
{
    //建筑的原石、施工、容量和产物参数由 XML 提供。
    public sealed class DefModExtension_AntStructure : DefModExtension
    {
        public int stoneChunks = 3;
        public float work = 1200f;
        public float radius = 7f;
        public int capacity = 16;
        public int productionTicks = 30000;
        public int productionCap = 75;
        public int productionPerPlant = 3;
        public int regrowthTicks = 5000;
        public float growthRadius = 5f;
        public float growthMultiplier = 2f;

        //拒绝不能施工或不能正常生产的配置。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (stoneChunks < 1 || work <= 0 || radius <= 0) yield return "蚁群建筑的原石数量、工作量和范围必须大于零。";
            if (capacity < 1 || productionTicks < 1 || productionCap < 1 || regrowthTicks < 1)
                yield return "蚁群容器容量和生产间隔必须大于零。";
        }
    }
}
