using HarmonyLib;
using NingshaRaceLib.SandGolem.Utility;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.SandGolem.Patches
{
    //把独立工作的沙傀计入可用医生，避免病人在等待治疗时反复起床。
    [HarmonyPatch(typeof(WorkGiver_PatientGoToBedTreatment), nameof(WorkGiver_PatientGoToBedTreatment.AnyAvailableDoctorFor))]
    internal static class SandGolemDoctorPatch
    {
        //沿用原版医生的工作、健康和可达性条件，不要求机械师控制组。
        private static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result || pawn.MapHeld == null) return;
            foreach (Pawn doctor in pawn.MapHeld.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (doctor != pawn && SandGolemUtility.IsSandGolem(doctor)
                    && !doctor.Dead && !doctor.Downed && doctor.Awake() && !doctor.InBed()
                    && !doctor.InMentalState && !doctor.IsPrisoner
                    && !SandGolemUtility.IsMovementLockedSandGolem(doctor)
                    && doctor.workSettings != null && doctor.workSettings.EverWork
                    && doctor.workSettings.WorkIsActive(WorkTypeDefOf.Doctor)
                    && doctor.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                    && doctor.CanReach(pawn, PathEndMode.Touch, Danger.Deadly))
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}
