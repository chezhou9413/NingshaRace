using HarmonyLib;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Utility;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Ecology.Patches
{
    //沙漠洞穴的动物离场时只选择返回地表的出口。
    [HarmonyPatch]
    internal static class DesertPitAnimalExitPatch
    {
        //原版离场任务会选任意传送门，在巨坑中替换为地表出口。
        [HarmonyPrefix]
        [HarmonyPatch(typeof(JobGiver_ExitMap), "TryGiveJob")]
        private static bool GiveSurfaceExitJob(Pawn pawn, ref Job __result)
        {
            if (!UsesSurfaceExit(pawn)) return true;
            MapPortal exit = pawn.Downed && !pawn.Crawling ? null : FindSurfaceExit(pawn);
            __result = exit == null ? null : JobMaker.MakeJob(JobDefOf.EnterPortal, exit);
            return false;
        }

        //其他动物离场逻辑也不能把深入巢穴的传送门视为地图出口。
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RCellFinder), nameof(RCellFinder.TryFindExitPortal))]
        private static bool FindSurfaceExitPortal(Pawn pawn, ref Thing portal, ref bool __result)
        {
            if (!UsesSurfaceExit(pawn)) return true;
            portal = FindSurfaceExit(pawn);
            __result = portal != null;
            return false;
        }

        //仅接管沙漠巨坑主地图中的动物离场。
        private static bool UsesSurfaceExit(Pawn pawn)
        {
            return pawn.RaceProps.Animal && DesertPitMapUtility.IsDesertPitMap(pawn.MapHeld);
        }

        //选择最近且可进入、可走到的洞穴地表出口；受阻时等待出口恢复。
        private static MapPortal FindSurfaceExit(Pawn pawn)
        {
            MapPortal nearest = null;
            int nearestDistance = int.MaxValue;
            foreach (Thing thing in pawn.MapHeld.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (!(thing is Building_NingshaCaveExit exit) || !exit.IsEnterable(out _)
                    || !pawn.CanReach(exit, PathEndMode.Touch, Danger.Deadly)) continue;
                int distance = pawn.Position.DistanceToSquared(exit.Position);
                if (distance >= nearestDistance) continue;
                nearest = exit;
                nearestDistance = distance;
            }
            return nearest;
        }
    }
}
