using HarmonyLib;
using NingshaRaceLib.DesertPit.AntColony.Components;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Patches
{
    //死亡前捕获地图，尸体生成后只记录一次死亡。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_AntFatalDamage
    {
        //致命伤也能触发蚁后警报，死亡后 Pawn 可能已经离开地图。
        public static void Prefix(Pawn __instance, DamageInfo? dinfo, out Map __state)
        {
            __state = null;
            if (!__instance.Spawned || __instance.Dead || !(__instance.GetComp<Comp_DesertPitAntMember>()?.ColonyId > 0)) return;
            __state = __instance.Map;
            __state.GetComponent<MapComponent_DesertPitAntColonies>().NotifyMemberDamaged(__instance, dinfo?.Instigator);
        }

        //成员销毁回调与此处重复时，管理器会根据索引去重。
        public static void Postfix(Pawn __instance, Map __state)
        {
            if (__state != null && __instance.Dead) __state.GetComponent<MapComponent_DesertPitAntColonies>()
                .NotifyMemberDestroyed(__instance, __instance.GetComp<Comp_DesertPitAntMember>().ColonyId);
        }
    }
}
