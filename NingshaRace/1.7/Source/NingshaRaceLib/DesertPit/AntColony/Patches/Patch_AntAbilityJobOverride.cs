using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NingshaRaceLib.DesertPit.AntColony.Combat;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Patches
{
    //蓄力和位移期间拒绝换工作及挂起，完成后再响应新任务。
    [HarmonyPatch]
    internal static class Patch_AntAbilityJobOverride
    {
        //覆盖行为树重新派活和临时挂起的入口。
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.CheckForJobOverride));
            yield return AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.SuspendCurrentJob));
        }

        //自然完成、死亡和倒地后自动解除锁定。
        private static bool Prefix(Pawn_JobTracker __instance) => !AntAbilityCommitment.IsLocked(__instance.curDriver);
    }
}
