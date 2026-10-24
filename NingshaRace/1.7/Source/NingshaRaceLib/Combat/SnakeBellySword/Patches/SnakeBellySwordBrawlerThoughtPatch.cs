using HarmonyLib;
using RimWorld;
using Verse;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Patches
{
    //远距离挥击仍属于近战武器，不触发格斗者的远程武器心情。
    [HarmonyPatch(typeof(ThoughtWorker_IsCarryingRangedWeapon), "CurrentStateInternal")]
    internal static class SnakeBellySwordBrawlerThoughtPatch
    {
        //仅排除蛇腹剑，保留其他武器的原版判定。
        private static void Postfix(Pawn p, ref ThoughtState __result)
        {
            if (p.equipment?.Primary?.def == DefOfRefs.NingshaRace_SnakeBellySword)
                __result = ThoughtState.Inactive;
        }
    }
}
