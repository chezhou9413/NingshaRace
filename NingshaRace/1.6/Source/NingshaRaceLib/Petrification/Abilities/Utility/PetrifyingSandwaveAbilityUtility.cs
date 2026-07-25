using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Health;
using NingshaRaceLib.Petrification.Patches;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.Petrification.Utility;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.Petrification.Abilities.Utility
{
    //类职责：负责给玩家凝砂族 Pawn 补齐石化砂潮能力。
    public static class PetrifyingSandwaveAbilityUtility
    {
        //函数职责：在符合种族和阵营条件时添加唯一的石化砂潮能力。
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

            if (pawn.abilities.GetAbility(DefOfRefs.NingshaRace_Ability_PetrifyingSandwave) == null)
            {
                pawn.abilities.GainAbility(DefOfRefs.NingshaRace_Ability_PetrifyingSandwave);
            }
        }
    }
}
