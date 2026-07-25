using Verse;

namespace NingshaRaceLib.SandGolem.Health
{
    //类职责：声明沙傀标记 Hediff 使用的组件类型并绑定对应实现。
    public class HediffCompProperties_SandGolemMarker : HediffCompProperties
    {
        //构造函数职责：绑定沙傀标记组件实现。
        public HediffCompProperties_SandGolemMarker()
        {
            compClass = typeof(HediffComp_SandGolemMarker);
        }
    }
}
