using HarmonyLib;
using NingshaRaceLib.Abilities;
using Verse;

namespace NingshaRaceLib.Patches
{
    //类职责：在 Pawn 生成到地图后给玩家凝砂族补齐召唤沙傀能力。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_Pawn_SpawnSetup_AbilityGrant
    {
        //函数职责：Pawn 生成后执行能力补齐。
        public static void Postfix(Pawn __instance)
        {
            SandGolemAbilityUtility.EnsureAbility(__instance);
        }
    }

    //类职责：低频 Tick 补齐已有玩家凝砂族的召唤沙傀能力。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TickRare))]
    public static class Patch_Pawn_TickRare_AbilityGrant
    {
        //函数职责：Pawn 稀疏 Tick 时执行能力补齐。
        public static void Postfix(Pawn __instance)
        {
            SandGolemAbilityUtility.EnsureAbility(__instance);
        }
    }
}
