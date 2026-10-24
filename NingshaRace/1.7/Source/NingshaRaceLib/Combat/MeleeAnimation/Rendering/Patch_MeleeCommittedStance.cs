using HarmonyLib;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //原版工作结束和反击重选不能清除已经出手的动作。
    [HarmonyPatch(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance))]
    internal static class Patch_MeleeCommittedStance
    {
        //仅保护本框架的一次攻击，死亡、倒地和武器离手仍允许正常清理。
        private static bool Prefix(Pawn_StanceTracker __instance, Stance newStance)
            => !(__instance.curStance is Stance_MeleeAnimation committed) || committed.AllowReplacement(newStance);
    }
}
