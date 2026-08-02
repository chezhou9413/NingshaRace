using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Genes.Utility;
using NingshaRaceLib.Petrification.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Genes.Patches
{
    //类职责：在原版近战命中完成后为毒牙基因执行一次石化进度判定。
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Patch_VenomFangsMeleeHit
    {
        private const float TriggerChance = 0.5f;
        private const float PetrificationSeverity = 0.05f;

        //函数职责：对携带活动毒牙基因的攻击者命中的存活血肉 Pawn 累计石化进度。
        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            Pawn targetPawn = target.Pawn;
            if (!NingshaGeneUtility.HasActiveGene(__instance.CasterPawn, DefOfRefs.NingshaRace_VenomFangs)
                || targetPawn == null
                || targetPawn.Dead
                || !targetPawn.RaceProps.IsFlesh
                || !Rand.Chance(TriggerChance))
            {
                return;
            }

            PetrificationUtility.AddSeverity(targetPawn, PetrificationSeverity);
        }
    }
}
