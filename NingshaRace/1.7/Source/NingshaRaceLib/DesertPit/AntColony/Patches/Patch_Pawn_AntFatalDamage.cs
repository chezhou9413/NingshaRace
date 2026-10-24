using HarmonyLib;
using NingshaRaceLib.DesertPit.AntColony.Components;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Patches
{
    //类职责：在致命一击移除成员前记录攻击来源，让单次致死攻击同样触发有限反击。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_AntFatalDamage
    {
        //函数职责：仅通知已登记巢群的存活成员，不干预原版死亡、尸体及伤亡计数流程。
        public static void Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            if (!__instance.Spawned || __instance.Dead || !dinfo.HasValue || dinfo.Value.Instigator == null) return;
            if (__instance.TryGetComp<Comp_DesertPitAntMember>()?.ColonyId > 0)
                __instance.Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyMemberDamaged(__instance, dinfo.Value.Instigator);
        }
    }
}
