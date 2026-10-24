using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.SnakeBellySword.Patches
{
    //让格斗者装备警报与蛇腹剑的心情判定一致。
    [HarmonyPatch(typeof(Alert_BrawlerHasRangedWeapon), "BrawlersWithRangedWeapon", MethodType.Getter)]
    internal static class SnakeBellySwordBrawlerAlertPatch
    {
        //移除当前握持蛇腹剑的殖民者。
        private static void Postfix(List<Pawn> __result)
        {
            __result.RemoveAll(pawn => pawn.equipment?.Primary?.def == DefOfRefs.NingshaRace_SnakeBellySword);
        }
    }
}
