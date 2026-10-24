using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Salvage.Components
{
    //类职责：配置洞穴物件拆除时互斥抽取的资源奖励列表。
    public sealed class CompProperties_DesertPitSalvage : CompProperties
    {
        //字段职责：保存全部拆除奖励及其数量和百分比权重。
        public List<DesertPitSalvageOption> options = new List<DesertPitSalvageOption>();

        //构造函数职责：绑定负责实际生成拆除奖励的运行组件。
        public CompProperties_DesertPitSalvage()
        {
            compClass = typeof(CompDesertPitSalvage);
        }

        //函数职责：在 Def 加载时校验奖励内容完整且权重总和严格为一百。
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (options == null || options.Count == 0)
            {
                yield return parentDef.defName + " 缺少洞穴拆除奖励配置。";
                yield break;
            }

            float totalWeight = 0f;
            for (int i = 0; i < options.Count; i++)
            {
                DesertPitSalvageOption option = options[i];
                if (option == null || option.thingDef == null)
                {
                    yield return parentDef.defName + " 的第 " + i + " 项洞穴拆除奖励缺少资源定义。";
                    continue;
                }
                if (option.count <= 0)
                {
                    yield return parentDef.defName + " 的 " + option.thingDef.defName + " 拆除奖励数量必须大于零。";
                }
                if (option.weight <= 0f)
                {
                    yield return parentDef.defName + " 的 " + option.thingDef.defName + " 拆除奖励权重必须大于零。";
                }
                totalWeight += option.weight;
            }

            if (Mathf.Abs(totalWeight - 100f) > 0.001f)
            {
                yield return parentDef.defName + " 的洞穴拆除奖励权重总和必须为 100，当前为 " + totalWeight + "。";
            }
        }
    }
}
