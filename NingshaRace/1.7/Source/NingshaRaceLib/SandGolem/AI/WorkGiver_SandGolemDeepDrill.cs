using NingshaRaceLib.SandGolem.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.SandGolem.AI
{
    //沙傀专用深钻入口，复用原版电力、矿藏、预订与工作逻辑。
    public sealed class WorkGiver_SandGolemDeepDrill : WorkGiver_DeepDrill
    {
        //仅沙傀可接取，不改变其他机械体的工作许可。
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
            => !SandGolemUtility.IsSandGolem(pawn) || base.ShouldSkip(pawn, forced);

        //右键派工也必须遵循沙傀限定。
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
            => SandGolemUtility.IsSandGolem(pawn) && base.HasJobOnThing(pawn, t, forced);
    }
}
