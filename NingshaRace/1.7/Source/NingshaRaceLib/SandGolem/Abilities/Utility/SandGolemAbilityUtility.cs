using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Abilities.Utility
{
    //类职责：负责给玩家凝砂族 Pawn 补齐召唤沙傀能力。
    public static class SandGolemAbilityUtility
    {
        //函数职责：在符合条件时给 Pawn 添加召唤沙傀能力。
        public static void EnsureAbility(Pawn pawn)
        {
            if (!SandGolemUtility.IsPlayerNingshaPawn(pawn) || SandGolemUtility.IsSandGolem(pawn))
            {
                return;
            }

            if (pawn.abilities == null)
            {
                pawn.abilities = new Pawn_AbilityTracker(pawn);
            }

            if (pawn.abilities.GetAbility(DefOfRefs.NingshaRace_Ability_SummonSandGolem) == null)
            {
                pawn.abilities.GainAbility(DefOfRefs.NingshaRace_Ability_SummonSandGolem);
            }
        }
    }
}
