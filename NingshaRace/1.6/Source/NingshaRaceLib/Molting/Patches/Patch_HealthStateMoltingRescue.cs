using HarmonyLib;
using Verse;

using NingshaRaceLib.Molting.Health;

namespace NingshaRaceLib.Molting.Patches
{
    //类职责：在原版健康状态完成一次检查后触发凝砂族伤势保命判定。
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.CheckForStateChange))]
    public static class Patch_HealthStateMoltingRescue
    {
        //字段职责：读取原版健康追踪器私有Pawn引用而不改写原版状态。
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        //函数职责：把当前健康追踪器对应Pawn交给带重入锁的保命工具。
        public static void Postfix(Pawn_HealthTracker __instance)
        {
            MoltingRescueUtility.TryResolve(PawnRef(__instance));
        }
    }
}
