using Verse;

namespace NingshaRaceLib.Altar.Components
{
    //类职责：配置智慧之蛇祭坛可保存的最大生肉营养供奉值。
    public sealed class CompProperties_AltarOffering : CompProperties
    {
        //字段职责：规定祭坛充满并允许发布任务所需的营养值。
        public float nutritionCapacity = 100f;

        //构造函数职责：绑定祭坛供奉运行组件类型。
        public CompProperties_AltarOffering()
        {
            compClass = typeof(CompAltarOffering);
        }
    }
}
