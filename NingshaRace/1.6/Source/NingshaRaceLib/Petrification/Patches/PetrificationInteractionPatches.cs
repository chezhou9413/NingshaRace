using HarmonyLib;
using NingshaRaceLib.Petrification.Utility;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.Petrification.Patches
{
    //保留卧床姿态时同时暂停逐帧工作，防止继续治疗、进食或休息任务的动作。
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.JobTrackerTick))]
    internal static class PetrificationJobTickPatch
    {
        //完全石化期间不推进当前工作的 Toil。
        private static bool Prefix(Pawn ___pawn) => !PetrificationUtility.IsFullyPetrified(___pawn);
    }

    //石化角色不能主动交谈。
    [HarmonyPatch(typeof(SocialInteractionUtility), nameof(SocialInteractionUtility.CanInitiateInteraction))]
    internal static class PetrificationInitiateInteractionPatch
    {
        //保留原版其他社交条件，仅禁止完全石化者。
        private static void Postfix(Pawn pawn, ref bool __result)
        {
            if (PetrificationUtility.IsFullyPetrified(pawn)) __result = false;
        }
    }

    //石化角色不能作为社交互动的接收者。
    [HarmonyPatch(typeof(SocialInteractionUtility), nameof(SocialInteractionUtility.CanReceiveInteraction))]
    internal static class PetrificationReceiveInteractionPatch
    {
        //避免其他角色继续对石像聊天。
        private static void Postfix(Pawn pawn, ref bool __result)
        {
            if (PetrificationUtility.IsFullyPetrified(pawn)) __result = false;
        }
    }
}
