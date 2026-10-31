using System.Collections.Generic;
using HarmonyLib;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Components;
using NingshaRaceLib.DesertPit.AntColony.Core;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Patches
{
    //普通蚂蚁屠宰和风干共用甲壳数量，爆浆蚁不进入尸体流程。
    public static class AntCarapaceProducts
    {
        //按种类固定产量，不把体型倍率重复施加到甲壳数量。
        public static int Count(Pawn pawn)
        {
            Comp_DesertPitAntMember member = pawn.GetComp<Comp_DesertPitAntMember>();
            if (member == null) return 0;
            return member.Caste switch { AntCaste.Worker => 2, AntCaste.Breaker => 5, AntCaste.Queen => 8,
                AntCaste.Boom => 0, _ => 3 };
        }
    }

    //保留原版肉类产物并额外添加甲壳。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ButcherProducts))]
    public static class Patch_AntButchery
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> values, Pawn __instance)
        {
            foreach (Thing value in values) yield return value;
            int count = AntCarapaceProducts.Count(__instance);
            if (count == 0) yield break;
            Thing shell = ThingMaker.MakeThing(DefOfRefs.NingshaRace_BlackCarapace);
            shell.stackCount = count;
            yield return shell;
        }
    }

    //风干时以甲壳替换原尸体，容器内外均保持正确持有关系。
    [HarmonyPatch(typeof(CompRottable), "TickInterval")]
    public static class Patch_AntDessication
    {
        public static void Postfix(CompRottable __instance)
        {
            if (!(__instance.parent is Corpse corpse) || corpse.Destroyed || corpse.GetRotStage() != RotStage.Dessicated) return;
            int count = AntCarapaceProducts.Count(corpse.InnerPawn);
            if (count == 0) return;
            Map map = corpse.MapHeld;
            IntVec3 cell = corpse.PositionHeld;
            ThingOwner owner = corpse.holdingOwner;
            Thing shell = ThingMaker.MakeThing(DefOfRefs.NingshaRace_BlackCarapace);
            shell.stackCount = count;
            corpse.Destroy();
            bool placed = owner != null ? owner.TryAdd(shell) : map != null && GenPlace.TryPlaceThing(shell, cell, map, ThingPlaceMode.Near);
            if (!placed) throw new System.InvalidOperationException("风干蚂蚁的甲壳无法释放。");
        }
    }

    //成员取食后使所属巢群账目立即失效。
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_AntFoodIntake
    {
        public static void Postfix(Pawn ingester)
        {
            Comp_DesertPitAntMember member = ingester.GetComp<Comp_DesertPitAntMember>();
            if (member != null && ingester.Spawned)
                ingester.Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyInventoryChanged(member.ColonyId);
        }
    }
}
