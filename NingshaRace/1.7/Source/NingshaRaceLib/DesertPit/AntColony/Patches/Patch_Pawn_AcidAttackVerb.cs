using HarmonyLib;
using NingshaRaceLib.DesertPit.AntColony.Components;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Patches
{
    //类职责：为吐酸蚁接入原版攻击选择入口，使独立行为和调查队都能使用原生喷酸。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    public static class Patch_Pawn_AcidAttackVerb
    {
        //函数职责：保留贴身近战，非贴身时只为具有吐酸职责的蚂蚁选择可用远程攻击。
        public static void Postfix(Pawn __instance, Thing target, ref Verb __result)
        {
            if (target != null && __instance.Position.AdjacentTo8WayOrInside(target.Position)) return;
            Verb acid = MapComponent_DesertPitAntColonies.AcidVerb(__instance);
            if (acid != null) __result = acid;
        }
    }
}
