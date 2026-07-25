using RimWorld;
using Verse;

using NingshaRaceLib.Combat.BurialMountainGuard.Components;
using NingshaRaceLib.Combat.BurialMountainGuard.Rendering;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.BurialMountainGuard.Utility
{
    //类职责：集中提供葬岳格挡模式的装备识别、攻击拦截和提示文本。
    public static class BurialMountainGuardUtility
    {
        public const string GuardDisabledReason = "葬岳格挡模式中无法攻击";

        //函数职责：从 Pawn 当前主武器上读取葬岳格挡 Comp。
        public static bool TryGetGuardComp(Pawn pawn, out Comp_BurialMountainGuardMode comp)
        {
            comp = null;
            ThingWithComps weapon = pawn?.equipment?.Primary;
            if (weapon == null || weapon.def != DefOfRefs.NingshaRace_BurialMountainGreatsword)
            {
                return false;
            }

            comp = weapon.GetComp<Comp_BurialMountainGuardMode>();
            return comp != null;
        }

        //函数职责：判断 Pawn 是否正在使用葬岳格挡模式。
        public static bool IsGuarding(Pawn pawn)
        {
            Comp_BurialMountainGuardMode comp;
            return TryGetGuardComp(pawn, out comp) && comp.GuardMode;
        }

        //函数职责：只拦截格挡期间的普通近战，并明确允许坠岳斩能力。
        public static bool ShouldBlockVerb(Verb verb)
        {
            if (verb == null)
            {
                return false;
            }

            Verb_CastAbility abilityVerb = verb as Verb_CastAbility;
            if (abilityVerb != null
                && abilityVerb.ability != null
                && abilityVerb.ability.def == DefOfRefs.NingshaRace_Ability_FallingMountainSlash)
            {
                return false;
            }

            return verb.IsMeleeAttack;
        }
    }
}
