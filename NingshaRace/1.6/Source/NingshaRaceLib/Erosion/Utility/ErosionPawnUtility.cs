using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Erosion.Utility
{
    //类职责：集中判断侵蚀系统适用对象，并只操作凝砂族的两项固有能力。
    public static class ErosionPawnUtility
    {
        //函数职责：判断 Pawn 是否为尚未实体化的玩家凝砂族。
        public static bool IsNormalPlayerNingsha(Pawn pawn)
        {
            return pawn != null
                && pawn.def == DefOfRefs.NingshaRace
                && pawn.Faction == Faction.OfPlayer
                && !pawn.IsMutant;
        }

        //函数职责：判断 Pawn 是否已经永久转化为凝砂族侵蚀体。
        public static bool IsErosionBody(Pawn pawn)
        {
            return pawn != null
                && pawn.def == DefOfRefs.NingshaRace
                && pawn.IsMutant
                && pawn.mutant.Def == DefOfRefs.NingshaRace_ErosionBodyMutant;
        }

        //函数职责：判断凝砂之眼或召唤沙傀是否至少有一项仍处于冷却。
        public static bool HasInnateAbilityOnCooldown(Pawn pawn)
        {
            if (pawn?.abilities == null)
            {
                return false;
            }

            Ability petrifyingEye = pawn.abilities.GetAbility(DefOfRefs.NingshaRace_Ability_PetrifyingSandwave);
            Ability summonGolem = pawn.abilities.GetAbility(DefOfRefs.NingshaRace_Ability_SummonSandGolem);
            return petrifyingEye?.OnCooldown == true || summonGolem?.OnCooldown == true;
        }

        //函数职责：清除凝砂之眼与召唤沙傀的全部冷却，不触碰其他来源的能力。
        public static void ResetInnateAbilityCooldowns(Pawn pawn)
        {
            if (pawn?.abilities == null)
            {
                return;
            }

            pawn.abilities.GetAbility(DefOfRefs.NingshaRace_Ability_PetrifyingSandwave)?.ResetCooldown();
            pawn.abilities.GetAbility(DefOfRefs.NingshaRace_Ability_SummonSandGolem)?.ResetCooldown();
        }
    }
}
