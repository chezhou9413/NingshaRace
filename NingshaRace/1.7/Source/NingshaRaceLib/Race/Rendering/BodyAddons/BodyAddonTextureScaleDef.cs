using System.Collections.Generic;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Race.Components;

namespace NingshaRaceLib.Race.Rendering.BodyAddons
{
    //类职责：为指定种族集中声明按 BodyAddon 贴图和朝向生效的缩放与偏移规则。
    public sealed class BodyAddonTextureScaleDef : Def
    {
        //字段职责：指定当前配置作用的 Pawn 种族。
        public ThingDef race;

        //字段职责：保存当前种族需要覆盖默认 BodyAddon 变换的规则。
        public List<BodyAddonTextureScaleRule> rules = new List<BodyAddonTextureScaleRule>();

        //函数职责：检查目标种族、规则必填项、数值范围和重复键。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (race == null)
            {
                yield return defName + ".race 不能为空";
            }
            if (rules == null || rules.Count == 0)
            {
                yield return defName + " 至少需要一条 rules 配置";
                yield break;
            }

            HashSet<string> ruleKeys = new HashSet<string>();
            for (int index = 0; index < rules.Count; index++)
            {
                BodyAddonTextureScaleRule rule = rules[index];
                if (rule == null)
                {
                    yield return defName + ".rules[" + index + "] 不能为空";
                    continue;
                }

                foreach (string error in rule.ConfigErrors(defName, index))
                {
                    yield return error;
                }

                if (!ruleKeys.Add(rule.BuildKey()))
                {
                    yield return defName + ".rules[" + index + "] 与已有规则重复";
                }
            }
        }
    }
}
