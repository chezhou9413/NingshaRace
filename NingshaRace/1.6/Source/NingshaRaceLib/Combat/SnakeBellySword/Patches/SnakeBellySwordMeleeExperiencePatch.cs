using HarmonyLib;
using RimWorld;
using Verse;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Patches
{
    //蛇腹剑切换为贴身砍墙时也获得近战经验。
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    internal static class SnakeBellySwordMeleeExperiencePatch
    {
        //原版已经奖励对活动 Pawn 的攻击，这里只补建筑命中。
        private static void Postfix(Verb_MeleeAttack __instance, bool __result)
        {
            if (!__result || __instance.EquipmentSource?.def != DefOfRefs.NingshaRace_SnakeBellySword
                || __instance.CurrentTarget.Thing?.def.category != ThingCategory.Building) return;
            Pawn pawn = __instance.CasterPawn;
            pawn.skills?.Learn(SkillDefOf.Melee, 200f * __instance.verbProps.AdjustedFullCycleTime(__instance, pawn));
        }
    }
}
