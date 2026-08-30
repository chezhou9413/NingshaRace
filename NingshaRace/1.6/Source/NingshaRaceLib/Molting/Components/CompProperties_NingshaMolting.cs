using Verse;

namespace NingshaRaceLib.Molting.Components
{
    //类职责：配置凝砂族蜕皮营养容量与防死亡消耗阈值。
    public sealed class CompProperties_NingshaMolting : CompProperties
    {
        //字段职责：规定一次主动蜕皮需要的营养值和可保存上限。
        public float nutritionCapacity = 100f;

        //字段职责：规定触发一次伤势保命需要消耗的营养值。
        public float rescueNutritionCost = 60f;

        //构造函数职责：绑定凝砂族蜕皮运行组件。
        public CompProperties_NingshaMolting()
        {
            compClass = typeof(CompNingshaMolting);
        }
    }
}
