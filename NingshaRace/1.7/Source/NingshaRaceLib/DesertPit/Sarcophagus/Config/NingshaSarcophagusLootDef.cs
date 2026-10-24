using System.Collections.Generic;
using NingshaRaceLib.GiantTomb.Content.Config;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Sarcophagus.Config
{
    //类职责：配置封闭砂岩石棺中的古老尸体与独立奖励池。
    public sealed class NingshaSarcophagusLootDef : Def
    {
        public PawnKindDef corpseKind;
        public FactionDef corpseFaction;
        public IntRange corpseAgeYears;
        public IntRange rewardPickCount;
        public bool rewardWithReplacement;
        public List<NingshaWeightedThingEntry> rewards = new List<NingshaWeightedThingEntry>();

        //函数职责：在Def加载阶段验证尸体、尸龄、抽取次数和奖励池。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (corpseKind == null)
            {
                yield return defName + ": corpseKind不能为空";
            }
            if (corpseFaction == null)
            {
                yield return defName + ": corpseFaction不能为空";
            }
            if (corpseAgeYears.min < 0 || corpseAgeYears.max < corpseAgeYears.min)
            {
                yield return defName + ": corpseAgeYears无效";
            }
            if (rewardPickCount.min < 1 || rewardPickCount.max < rewardPickCount.min)
            {
                yield return defName + ": rewardPickCount无效";
            }
            if (rewards == null || rewards.Count == 0)
            {
                yield return defName + ": rewards不能为空";
                yield break;
            }
            if (!rewardWithReplacement && rewardPickCount.max > rewards.Count)
            {
                yield return defName + ": 无放回抽取上限不能超过奖励种类数";
            }
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i] == null)
                {
                    yield return defName + ": rewards[" + i + "]不能为空";
                    continue;
                }
                foreach (string error in rewards[i].ConfigErrors(defName + ".rewards[" + i + "]"))
                {
                    yield return error;
                }
            }
        }
    }
}
