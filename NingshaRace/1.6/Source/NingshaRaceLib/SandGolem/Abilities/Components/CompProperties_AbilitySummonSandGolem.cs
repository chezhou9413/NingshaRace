using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NingshaRaceLib.SandGolem.Abilities.Components
{
    //类职责：声明召唤沙傀能力组件的属性类型并绑定对应实现。
    public class CompProperties_AbilitySummonSandGolem : CompProperties_AbilityEffect
    {
        //字段职责：声明允许召唤沙傀的地形列表。
        public List<TerrainDef> allowedTerrains;

        //构造函数职责：绑定召唤沙傀能力组件实现。
        public CompProperties_AbilitySummonSandGolem()
        {
            compClass = typeof(CompAbilityEffect_SummonSandGolem);
        }

        //函数职责：在 Def 加载时校验召唤地形配置完整且不包含空引用。
        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (allowedTerrains == null || allowedTerrains.Count == 0)
            {
                yield return parentDef.defName + " 的召唤沙傀组件必须配置 allowedTerrains。";
                yield break;
            }

            for (int i = 0; i < allowedTerrains.Count; i++)
            {
                if (allowedTerrains[i] == null)
                {
                    yield return parentDef.defName + " 的 allowedTerrains 第 " + i + " 项为空。";
                }
            }
        }
    }
}
