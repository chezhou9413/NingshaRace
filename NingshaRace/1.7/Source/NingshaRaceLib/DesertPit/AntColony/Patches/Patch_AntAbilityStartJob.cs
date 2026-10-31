using HarmonyLib;
using NingshaRaceLib.DesertPit.AntColony.Combat;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Patches
{
    //直接派发的工作同样等待位移技能结束，错误恢复仍交给原版。
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    internal static class Patch_AntAbilityStartJob
    {
        //原版错误恢复可以替换失败的技能，避免错误任务无法清理。
        private static bool Prefix(Pawn_JobTracker __instance, JobCondition lastJobEndCondition) =>
            lastJobEndCondition == JobCondition.Errored || lastJobEndCondition == JobCondition.ErroredPather
            || !AntAbilityCommitment.IsLocked(__instance.curDriver);
    }
}
