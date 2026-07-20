using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Rendering.BodyAddons
{
    //类职责：声明一个种族中需要跟随前一层变体编号并自动补透明方向的 BodyAddon 配对规则。
    public sealed class BodyAddonLinkFallbackExtension : DefModExtension
    {
        //字段职责：保存当前种族的 BodyAddon 链接透明回退规则。
        public List<BodyAddonLinkFallbackRule> rules = new List<BodyAddonLinkFallbackRule>();

        //函数职责：检查链接规则必填项和重复配对。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (rules == null || rules.Count == 0)
            {
                yield return "BodyAddonLinkFallbackExtension 至少需要一条 rules 配置";
                yield break;
            }

            HashSet<string> ruleKeys = new HashSet<string>();
            for (int index = 0; index < rules.Count; index++)
            {
                BodyAddonLinkFallbackRule rule = rules[index];
                if (rule == null)
                {
                    yield return "BodyAddonLinkFallbackExtension.rules[" + index + "] 不能为空";
                    continue;
                }

                foreach (string error in rule.ConfigErrors(index))
                {
                    yield return error;
                }

                if (!ruleKeys.Add(rule.BuildKey()))
                {
                    yield return "BodyAddonLinkFallbackExtension.rules[" + index + "] 与已有规则重复";
                }
            }
        }
    }
}
