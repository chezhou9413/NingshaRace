using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NingshaRaceLib.DesertPit.AntColony.Lords;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.AntColony.Patches
{
    //调查和复仇往返按兵蚁基准移动，交战时恢复各自速度。
    [HarmonyPatch]
    public static class Patch_AntMissionSpeed
    {
        //同时调整直线和斜向耗时，保留伤势与地形的其他修正。
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.TicksPerMoveCardinal));
            yield return AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.TicksPerMoveDiagonal));
        }

        //由兵蚁原始速度折算移动耗时，不改动 Pawn 的种类属性。
        public static void Postfix(Pawn __instance, ref float __result)
        {
            if (!(__instance.GetLord()?.LordJob is LordJob_DesertPitAntInvestigation mission) || !mission.Traveling) return;
            float ownSpeed = __instance.def.GetStatValueAbstract(StatDefOf.MoveSpeed);
            float soldierSpeed = NingshaRaceLib.Core.Defs.DefOfRefs.NingshaRace_DesertPitSoldierAntKind.race.GetStatValueAbstract(StatDefOf.MoveSpeed);
            __result *= ownSpeed / soldierSpeed;
        }
    }
}
