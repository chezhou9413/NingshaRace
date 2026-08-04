using RimWorld;
using Verse;

using NingshaRaceLib.Erosion.Components;

namespace NingshaRaceLib.Erosion.Thoughts
{
    //类职责：根据凝砂族当前侵蚀比例持续提供四阶段负面心情。
    public sealed class ThoughtWorker_NingshaErosion : ThoughtWorker
    {
        //函数职责：在侵蚀非零且尚未实体化时选择对应的心情阶段。
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            CompNingshaErosion erosion = pawn?.TryGetComp<CompNingshaErosion>();
            if (erosion == null || pawn.IsMutant || erosion.CurrentErosion <= 0f)
            {
                return ThoughtState.Inactive;
            }

            float ratio = erosion.ErosionRatio;
            if (ratio >= 0.75f)
            {
                return ThoughtState.ActiveAtStage(3);
            }
            if (ratio >= 0.5f)
            {
                return ThoughtState.ActiveAtStage(2);
            }
            if (ratio >= 0.25f)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            return ThoughtState.ActiveAtStage(0);
        }
    }
}
