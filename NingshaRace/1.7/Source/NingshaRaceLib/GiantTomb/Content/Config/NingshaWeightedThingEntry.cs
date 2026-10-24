using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.GiantTomb.Content.Config
{
    //类职责：声明一种可抽取物品、资源选择权重及其数量权重表。
    public sealed class NingshaWeightedThingEntry
    {
        public ThingDef thingDef;
        public bool localStoneBlocks;
        public float selectionWeight = 1f;
        public List<NingshaWeightedCount> quantities = new List<NingshaWeightedCount>();

        //函数职责：报告物品来源、选择权重和数量表中的配置错误。
        public IEnumerable<string> ConfigErrors(string owner)
        {
            if ((thingDef != null) == localStoneBlocks)
            {
                yield return owner + ": thingDef与localStoneBlocks必须且只能配置一个";
            }
            if (selectionWeight <= 0f)
            {
                yield return owner + ": selectionWeight必须大于零";
            }
            if (quantities == null || quantities.Count == 0)
            {
                yield return owner + ": quantities不能为空";
                yield break;
            }
            for (int i = 0; i < quantities.Count; i++)
            {
                if (quantities[i] == null)
                {
                    yield return owner + ": quantities[" + i + "]不能为空";
                    continue;
                }
                string error = quantities[i].ConfigError(owner + ".quantities[" + i + "]");
                if (error != null)
                {
                    yield return error;
                }
            }
        }
    }
}
