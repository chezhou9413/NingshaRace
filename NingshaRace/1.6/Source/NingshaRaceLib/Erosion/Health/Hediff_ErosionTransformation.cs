using Verse;

using NingshaRaceLib.Erosion.Components;

namespace NingshaRaceLib.Erosion.Health
{
    //类职责：标记满侵蚀实体化阶段，并在 Pawn 死亡时通知侵蚀组件终止动画资源。
    public sealed class Hediff_ErosionTransformation : HediffWithComps
    {
        //函数职责：死亡时取消尚未完成的侵蚀体转化。
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            pawn.TryGetComp<CompNingshaErosion>()?.CancelTransformation();
            base.Notify_PawnDied(dinfo, culprit);
        }
    }
}
