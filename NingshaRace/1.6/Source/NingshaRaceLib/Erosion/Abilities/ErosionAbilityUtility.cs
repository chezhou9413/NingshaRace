using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Abilities
{
    //类职责：负责给普通玩家凝砂族补齐侵蚀过载固有能力。
    public static class ErosionAbilityUtility
    {
        //函数职责：在符合种族、阵营和突变状态条件时添加唯一的侵蚀过载能力。
        public static void EnsureAbility(Pawn pawn)
        {
            if (!ErosionPawnUtility.IsNormalPlayerNingsha(pawn))
            {
                return;
            }

            if (pawn.abilities == null)
            {
                pawn.abilities = new Pawn_AbilityTracker(pawn);
            }

            if (pawn.abilities.GetAbility(DefOfRefs.NingshaRace_Ability_ErosionOverload) == null)
            {
                pawn.abilities.GainAbility(DefOfRefs.NingshaRace_Ability_ErosionOverload);
            }
        }
    }
}
