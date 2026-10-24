using HarmonyLib;
using Verse;
using NingshaRaceLib.Petrification.Utility;

namespace NingshaRaceLib.Petrification.Patches
{
    //类职责：冻结完全石化角色的倒地摆动与受击角度变化。
    [HarmonyPatch(typeof(PawnDownedWiggler))]
    public static class PetrificationWigglerPatch
    {
        //函数职责：阻止石化期间视觉更新改变倒地角度。
        [HarmonyPrefix, HarmonyPatch(nameof(PawnDownedWiggler.ProcessPostTickVisuals))]
        public static bool BeforeVisuals(Pawn ___pawn)
        {
            return !PetrificationUtility.IsFullyPetrified(___pawn);
        }

        //函数职责：阻止撞击让石化雕像继续转动。
        [HarmonyPrefix, HarmonyPatch(nameof(PawnDownedWiggler.Notify_DamageApplied))]
        public static bool BeforeDamage(Pawn ___pawn)
        {
            return !PetrificationUtility.IsFullyPetrified(___pawn);
        }
    }
}
