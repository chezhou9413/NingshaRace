using HarmonyLib;
using NingshaRaceLib.DesertPit.AntColony.Combat;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Patches
{
    //技能执行期间忽略主动中断，保留完成和错误清理。
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
    internal static class Patch_AntAbilityJobInterrupt
    {
        //受击、警报、撤退和巢毁先更新巢群状态，不取消已触发的动作。
        private static bool Prefix(Pawn_JobTracker __instance, JobCondition condition) =>
            condition != JobCondition.InterruptForced && condition != JobCondition.InterruptOptional
            || !AntAbilityCommitment.IsLocked(__instance.curDriver);
    }
}
