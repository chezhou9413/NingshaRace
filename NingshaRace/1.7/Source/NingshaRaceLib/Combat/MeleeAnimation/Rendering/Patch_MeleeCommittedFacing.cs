using HarmonyLib;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //一刀期间固定出招朝向，目标受击抖动或换工作不会反复切换四向曲线。
    [HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
    internal static class Patch_MeleeCommittedFacing
    {
        //动作结束后立即交回原版转向逻辑。
        private static bool Prefix(Pawn ___pawn)
        {
            if (!(___pawn.stances.curStance is Stance_MeleeAnimation committed) || !committed.Committed) return true;
            ___pawn.Rotation = committed.Facing;
            return false;
        }
    }
}
