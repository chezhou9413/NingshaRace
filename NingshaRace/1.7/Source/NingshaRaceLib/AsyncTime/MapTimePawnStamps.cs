using HarmonyLib;
using RimWorld;
using Verse;

namespace NingshaRaceLib.AsyncTime;

//换算明确使用游戏绝对 Tick 的常用字段，持续时长和生理年龄计数保持原值。
internal static class MapTimePawnStamps
{
    private static readonly AccessTools.FieldRef<Pawn_GuestTracker, int> EscapeTick =
        AccessTools.FieldRefAccess<Pawn_GuestTracker, int>("ticksWhenAllowedToEscapeAgain");

    //所有物体的生成记录与人物常用行为期限一起跨越时钟边界。
    internal static void Shift(Thing thing, int delta)
    {
        if (delta == 0) return;
        Add(ref thing.spawnedTick, delta, -1);
        Add(ref thing.despawnedTick, delta, -1);
        if (thing is not Pawn pawn) return;
        if (pawn.ageTracker != null) pawn.ageTracker.BirthAbsTicks += delta;
        var mind = pawn.mindState;
        if (mind != null)
        {
            Add(ref mind.canSleepTick, delta);
            Add(ref mind.canLovinTick, delta);
            Add(ref mind.lastIngestTick, delta);
            Add(ref mind.nextApparelOptimizeTick, delta);
            Add(ref mind.exitMapAfterTick, delta);
            Add(ref mind.lastDisturbanceTick, delta);
            Add(ref mind.hibernationEndedTick, delta);
            Add(ref mind.lastMeleeThreatHarmTick, delta);
            Add(ref mind.lastEngageTargetTick, delta);
            Add(ref mind.lastAttackTargetTick, delta);
            Add(ref mind.lastAssignedInteractTime, delta);
            Add(ref mind.lastDayInteractionTick, delta);
            Add(ref mind.lastInventoryRawFoodUseTick, delta);
            Add(ref mind.lastTakeCombatEnhancingDrugTick, delta);
            Add(ref mind.lastTakeRecreationalDrugTick, delta, -60000);
            Add(ref mind.lastHarmTick, delta);
            Add(ref mind.noAidRelationsGainUntilTick, delta);
            Add(ref mind.applyBedThoughtsTick, delta);
            Add(ref mind.applyThroneThoughtsTick, delta);
            Add(ref mind.lastSelfTendTick, delta);
            Add(ref mind.lastPredatorHuntingPlayerNotificationTick, delta);
            Add(ref mind.lastSlaveSuppressedTick, delta);
            Add(ref mind.lastHumanMeatIngestedTick, delta);
            Add(ref mind.nextInventoryStockTick, delta);
            Add(ref mind.nextSleepingBreastfeedStrictTick, delta);
            Add(ref mind.lastRangedHarmTick, delta);
            Add(ref mind.lastRotStinkTick, delta);
            Add(ref mind.lastBroughtToSafeTemperatureTick, delta);
            Add(ref mind.lastBecameVisibleTick, delta);
            Add(ref mind.lastBecameInvisibleTick, delta);
            Add(ref mind.lastForcedVisibleTick, delta);
            Add(ref mind.lastCombatantTick, delta);
            Add(ref mind.lastSwamTick, delta);
            if (mind.lastStartRoamCooldownTick.HasValue) mind.lastStartRoamCooldownTick += delta;
        }
        if (pawn.guest != null)
        {
            Add(ref EscapeTick(pawn.guest), delta, 0);
            Add(ref pawn.guest.lastPrisonBreakTicks, delta, -1);
        }
    }

    //保留原版尚未初始化的标记；合法的负时间戳仍应参与换算。
    private static void Add(ref int value, int delta, int sentinel = -99999)
    {
        if (value != sentinel) value = checked(value + delta);
    }
}
