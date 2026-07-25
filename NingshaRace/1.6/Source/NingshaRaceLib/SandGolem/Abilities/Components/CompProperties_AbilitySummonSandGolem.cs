using RimWorld;

namespace NingshaRaceLib.SandGolem.Abilities.Components
{
    //类职责：声明召唤沙傀能力组件的属性类型并绑定对应实现。
    public class CompProperties_AbilitySummonSandGolem : CompProperties_AbilityEffect
    {
        //构造函数职责：绑定召唤沙傀能力组件实现。
        public CompProperties_AbilitySummonSandGolem()
        {
            compClass = typeof(CompAbilityEffect_SummonSandGolem);
        }
    }
}
