using HarmonyLib;
using NingshaRaceLib.SandGolem;
using Verse;

namespace NingshaRaceLib.Patches
{
    //类职责：拦截沙傀死亡，让其播放消散动画而不是生成普通尸体。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_SandGolem
    {
        //函数职责：沙傀被击杀时启动消散并跳过原版死亡流程。
        public static bool Prefix(Pawn __instance)
        {
            if (!SandGolemUtility.IsSandGolem(__instance))
            {
                return true;
            }

            GameComponent_SandGolemTracker.Current?.BeginDissolve(__instance, destroyPawn: true);
            return false;
        }
    }
}
