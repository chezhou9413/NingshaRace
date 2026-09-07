using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Habitats
{
    //类职责：配置菌巢的植物覆盖范围、疏密间距、生长速度和持续补生规模。
    public sealed class DefModExtension_FungalMound : DefModExtension
    {
        public float radius = 5f;
        public float growthMultiplier = 2f;
        public float harvestMultiplier = 4f;
        //字段职责：限制范围内植物总数，空地或间距不足时不强行填满。
        public int targetPlants = 24;
        //字段职责：规定生成位置与附近植物的最小直线距离，零表示不额外限制间距。
        public float minimumPlantSpacing = 1.5f;
        public int regrowthIntervalTicks = 5000;
        public int regrowthBatch = 3;

        //函数职责：报告无法维持菌群的定义参数。
        public override IEnumerable<string> ConfigErrors()
        {
            if (radius <= 0f || growthMultiplier <= 0f || harvestMultiplier <= 0f || targetPlants <= 0 || regrowthIntervalTicks <= 0 || regrowthBatch <= 0)
                yield return "共生菌巢的范围、倍率、容量和补生参数必须为正数。";
            if (!(radius > 0f && radius < GenRadial.MaxRadialPatternRadius))
                yield return "共生菌巢的范围必须为正数，且小于原版圆形搜索支持的最大半径。";
            if (!(minimumPlantSpacing >= 0f && minimumPlantSpacing <= radius))
                yield return "共生菌巢的植物最小间距必须在零与作用范围之间。";
        }
    }
}
