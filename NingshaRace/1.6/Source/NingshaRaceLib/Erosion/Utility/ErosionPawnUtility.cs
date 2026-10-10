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

        //根据人形角色的异变身份识别侵蚀体，不限制原身种族。
        public static bool IsErosionBody(Pawn pawn)
        {
            return pawn != null
                && pawn.RaceProps.Humanlike
                && pawn.IsMutant
                && pawn.mutant.Def == DefOfRefs.NingshaRace_ErosionBodyMutant;
        }

        //只有凝砂族侵蚀体拥有蛇头及对应编辑入口。
        public static bool IsNingshaErosionBody(Pawn pawn)
        {
            return IsErosionBody(pawn) && pawn.def == DefOfRefs.NingshaRace;
        }

        //函数职责：在复活入口根据异变身份或永久健康标记识别侵蚀体原身。
        public static bool HasErosionBodyIdentity(Pawn pawn)
        {
            return pawn != null
                && pawn.RaceProps.Humanlike
                && (IsErosionBody(pawn)
                    || pawn.health.hediffSet.HasHediff(DefOfRefs.NingshaRace_ErosionBody));
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
