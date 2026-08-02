using RimWorld;
using Verse;

namespace NingshaRaceLib.Reproduction.Components
{
    //类职责：提供凝砂族周期排出未受精卵所需的 XML 可配置参数。
    public class CompProperties_NingshaEggCycle : CompProperties
    {
        //字段职责：记录两次未受精卵之间的基础间隔天数。
        public float eggLayingIntervalDays = 7f;

        //字段职责：指定周期完成后生成的未受精卵 Def。
        public ThingDef unfertilizedEggDef;

        //构造函数职责：把属性实例绑定到凝砂排卵组件。
        public CompProperties_NingshaEggCycle()
        {
            compClass = typeof(CompNingshaEggCycle);
        }
    }
}
