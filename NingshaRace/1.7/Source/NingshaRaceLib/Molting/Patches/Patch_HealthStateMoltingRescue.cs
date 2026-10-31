using HarmonyLib;
using Verse;

using NingshaRaceLib.Molting.Health;

namespace NingshaRaceLib.Molting.Patches
{
    //在原版死亡判定前处理可治疗伤势，致命缺损仍由原版结算。
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.CheckForStateChange))]
    public static class Patch_HealthStateMoltingRescue
    {
        //字段职责：读取原版健康追踪器私有Pawn引用而不改写原版状态。
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        //治疗过程会重入健康检查，交给工具的重入锁阻止中途死亡结算。
        public static bool Prefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = PawnRef(__instance);
            if (MoltingRescueUtility.IsResolving(pawn)) return false;
            MoltingRescueUtility.TryResolve(pawn);
            return true;
        }
    }
}
