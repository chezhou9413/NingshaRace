using HarmonyLib;
using RimWorld;

namespace NingshaRaceLib.SandGolem.Automation
{
    //类职责：征召时立即关闭自动召唤，防止同一帧取消征召后残留开关。
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
    public static class SandGolemAutoSummonDraftPatch
    {
        //函数职责：在征召控制器更新后清理该角色的自动召唤状态。
        public static void Postfix(Pawn_DraftController __instance)
        {
            if (__instance.Drafted && GameComponent_SandGolemAutoSummon.Current?.Enabled(__instance.pawn) == true)
                GameComponent_SandGolemAutoSummon.Current.Disable(__instance.pawn);
        }
    }
}
