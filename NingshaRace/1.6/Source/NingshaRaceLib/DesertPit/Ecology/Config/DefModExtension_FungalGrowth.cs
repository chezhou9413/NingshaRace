using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Config
{
    //类职责：配置菌类按成长率递进的幼体造型，超过最后一个阶段后使用原有成熟贴图。
    public sealed class DefModExtension_FungalGrowth : DefModExtension
    {
        public List<FungalGrowthStage> stages = new List<FungalGrowthStage>();

        //函数职责：按包含上限的连续区间选择造型，返回阶段总数表示成熟外观。
        public int StageIndex(float growth)
        {
            for (int i = 0; i < stages.Count; i++)
                if (growth <= stages[i].maxGrowth) return i;
            return stages.Count;
        }

        //函数职责：检查幼体阶段顺序、有效成长率上限和贴图路径，直接报告配置错误。
        public override IEnumerable<string> ConfigErrors()
        {
            if (stages == null || stages.Count == 0)
            {
                yield return "菌类生长外观必须配置幼体阶段。";
                yield break;
            }
            float previous = 0f;
            foreach (FungalGrowthStage stage in stages)
            {
                if (stage == null)
                {
                    yield return "菌类生长外观包含空阶段。";
                    continue;
                }
                if (!(stage.maxGrowth > previous && stage.maxGrowth < 1f))
                    yield return "菌类阶段成长率上限必须严格递增，且处于零与一之间。";
                if (string.IsNullOrWhiteSpace(stage.texPath))
                    yield return "菌类生长阶段缺少贴图路径。";
                previous = stage.maxGrowth;
            }
        }
    }
}
