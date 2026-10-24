using HarmonyLib;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.Antlion.Components;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NingshaRaceLib.DesertPit.Ecology.Patches
{
    //类职责：让常驻蚁群和蚁狮只在威胁玩家时参与战斗音乐判定。
    [HarmonyPatch(typeof(DangerWatcher), "AffectsStoryDanger")]
    public static class DesertPitStoryDangerPatch
    {
        //函数职责：保留原版威胁条件，并排除巡逻、采集和洞穴生物间的争斗。
        private static void Postfix(IAttackTarget t, ref bool __result)
        {
            if (!__result || !(t.Thing is Pawn pawn)) return;
            Comp_DesertPitAntMember ant = pawn.TryGetComp<Comp_DesertPitAntMember>();
            CompAntlionAmbush antlion = pawn.TryGetComp<CompAntlionAmbush>();
            if (ant == null && antlion == null) return;
            bool threatening = IsPlayerTarget(pawn.CurJob?.targetA.Thing, pawn.Map)
                || pawn.GetLord()?.CurLordToil?.ForceHighStoryDanger == true;
            if (antlion != null) threatening |= antlion.ThreatensPlayer;
            if (ant != null && pawn.Map.GetComponent<MapComponent_DesertPitAntColonies>().TryGetColony(pawn, out var state))
                threatening |= Find.TickManager.TicksGame < state.RetaliationUntilTick
                    && IsPlayerTarget(state.LastAggressor, pawn.Map);
            __result = threatening;
        }

        //函数职责：判断目标是否仍是同图活动的玩家实体。
        private static bool IsPlayerTarget(Thing target, Map map)
        {
            return target != null && target.Spawned && target.Map == map && target.Faction == Faction.OfPlayer;
        }
    }
}
