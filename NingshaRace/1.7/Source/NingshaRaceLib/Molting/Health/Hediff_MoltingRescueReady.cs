using Verse;

namespace NingshaRaceLib.Molting.Health
{
    //类职责：承载蜕皮保命就绪状态，并始终将该内部状态隐藏于健康面板。
    public sealed class Hediff_MoltingRescueReady : HediffWithComps
    {
        //属性职责：阻止内部就绪标记出现在玩家可见的健康状态列表中。
        public override bool Visible => false;
    }
}
