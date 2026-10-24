using RimWorld;

namespace NingshaRaceLib.Erosion.Abilities
{
    //类职责：保存侵蚀过载单次增加的侵蚀点数并绑定效果组件。
    public sealed class CompProperties_AbilityErosionOverload : CompProperties_AbilityEffect
    {
        //字段职责：定义每次过载增加的侵蚀点数。
        public float erosionGain = 20f;

        //构造函数职责：绑定侵蚀过载的能力效果实现。
        public CompProperties_AbilityErosionOverload()
        {
            compClass = typeof(CompAbilityEffect_ErosionOverload);
        }
    }
}
