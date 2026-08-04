using Verse;

namespace NingshaRaceLib.Erosion.Components
{
    //类职责：保存凝砂族侵蚀组件的自然衰减速度与实体化动画时长。
    public sealed class CompProperties_NingshaErosion : CompProperties
    {
        //字段职责：定义侵蚀值每天自然下降的点数。
        public float dailyDecay = 10f;

        //字段职责：定义达到侵蚀上限后完成实体化所需的 Tick 范围。
        public IntRange transformationTicks = new IntRange(300, 600);

        //构造函数职责：绑定凝砂族侵蚀状态的运行时组件。
        public CompProperties_NingshaErosion()
        {
            compClass = typeof(CompNingshaErosion);
        }
    }
}
