using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Reproduction.Components;
using NingshaRaceLib.Molting.Components;

namespace NingshaRaceLib.Reproduction.Utility
{
    //类职责：集中执行凝砂卵创建、孕期转卵、放置与破壳生成，供正式逻辑和调试入口共同复用。
    public static class NingshaReproductionUtility
    {
        //函数职责：判断指定 Pawn 是否为凝砂族成员。
        public static bool IsNingsha(Pawn pawn)
        {
            return pawn != null && pawn.def == DefOfRefs.NingshaRace;
        }

        //函数职责：取得凝砂 Pawn 身上的原版人类怀孕状态。
        public static Hediff_Pregnant GetHumanPregnancy(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
        }

        //函数职责：判断凝砂 Pawn 当前是否具有原版人类怀孕状态。
        public static bool HasHumanPregnancy(Pawn pawn)
        {
            return GetHumanPregnancy(pawn) != null;
        }

        //函数职责：判断凝砂 Pawn 是否正处于怀孕、阵痛或生产阶段。
        public static bool HasPregnancyOrLabor(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return false;
            }
            return pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman)
                || pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor)
                || pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLaborPushing);
        }

        //函数职责：创建指定 Def 的凝砂卵，并放到 Pawn 所在地图或玩家商队库存中。
        public static bool TryCreateAndPlaceEgg(Pawn pawn, ThingDef eggDef, out Thing egg)
        {
            if (pawn == null || eggDef == null)
            {
                Log.Error("[NingshaRace] 创建凝砂卵时缺少 Pawn 或卵 Def。");
                egg = null;
                return false;
            }

            egg = ThingMaker.MakeThing(eggDef);
            if (TryPlaceEggForPawn(pawn, egg))
            {
                return true;
            }

            egg.Destroy();
            egg = null;
            return false;
        }

        //函数职责：把已经创建的凝砂卵放到 Pawn 身边或其商队库存中。
        public static bool TryPlaceEggForPawn(Pawn pawn, Thing egg)
        {
            if (pawn.Spawned)
            {
                return GenPlace.TryPlaceThing(egg, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }
            if (pawn.IsCaravanMember() && pawn.inventory != null)
            {
                return pawn.inventory.innerContainer.TryAdd(egg);
            }
            return false;
        }

        //函数职责：创建一枚保存父母与母方阵营信息的受精凝砂卵。
        public static Thing CreateFertilizedEgg(Pawn mother, Pawn father, Faction faction)
        {
            Thing egg = ThingMaker.MakeThing(DefOfRefs.NingshaRace_EggFertilized);
            CompNingshaEmbryo embryo = egg.TryGetComp<CompNingshaEmbryo>();
            if (embryo == null)
            {
                Log.Error("[NingshaRace] 受精凝砂卵缺少 CompNingshaEmbryo。");
                egg.Destroy();
                return null;
            }
            embryo.Initialize(mother, father, faction);
            return egg;
        }

        //函数职责：把当前原版怀孕立即推进到开始阵痛的阈值。
        public static void ForcePregnancyToLabor(Pawn mother)
        {
            Hediff_Pregnant pregnancy = GetHumanPregnancy(mother);
            if (pregnancy == null)
            {
                Log.Error("[NingshaRace] 无法推进妊娠：目标没有原版人类怀孕状态。");
                return;
            }
            pregnancy.Severity = 1f;
        }

        //函数职责：供开发者命令使用当前怀孕记录立即产下受精卵。
        public static Thing CompleteCurrentPregnancyAsEggImmediately(Pawn mother, bool preventLetter)
        {
            Hediff_Pregnant pregnancy = GetHumanPregnancy(mother);
            Pawn father = pregnancy?.Father;
            return CreateBirthEgg(mother, father, preventLetter, clearPregnancyState: true);
        }

        //函数职责：在原版生产结算中生成受精卵，并把孕期状态清理完全交还原版调用栈。
        public static Thing CompleteOriginalBirthAsEgg(Pawn mother, Pawn father, bool preventLetter)
        {
            return CreateBirthEgg(mother, father, preventLetter, clearPregnancyState: false);
        }

        //函数职责：集中创建生产受精卵，并按调用来源决定是否主动清理尚未进入生产的孕期。
        private static Thing CreateBirthEgg(Pawn mother, Pawn father, bool preventLetter, bool clearPregnancyState)
        {
            if (!IsNingsha(mother))
            {
                Log.Error("[NingshaRace] 只有凝砂族能够通过凝砂生产流程排出受精卵。");
                return null;
            }

            Thing egg = CreateFertilizedEgg(mother, father, mother.Faction);
            if (egg == null || !TryPlaceEggForPawn(mother, egg))
            {
                egg?.Destroy();
                Log.Error("[NingshaRace] 受精凝砂卵没有可用的地图位置或商队库存。");
                return null;
            }

            if (!mother.health.hediffSet.HasHediff(HediffDefOf.PostpartumExhaustion))
            {
                mother.health.AddHediff(HediffDefOf.PostpartumExhaustion);
            }
            if (clearPregnancyState)
            {
                ClearPregnancyForImmediateEgg(mother);
            }

            if (!preventLetter && PawnUtility.ShouldSendNotificationAbout(mother))
            {
                string fatherName = father?.LabelShort ?? "NingshaRace_UnknownFather".Translate();
                Find.LetterStack.ReceiveLetter(
                    "NingshaRace_EggBirthLetterLabel".Translate(),
                    "NingshaRace_EggBirthLetterText".Translate(mother.LabelShort, fatherName),
                    LetterDefOf.PositiveEvent,
                    egg);
            }
            return egg;
        }

        //函数职责：仅为开发者立即产卵命令移除尚未进入最终生产的怀孕与早期阵痛状态。
        private static void ClearPregnancyForImmediateEgg(Pawn mother)
        {
            List<Hediff> hediffs = mother.health.hediffSet.hediffs
                .Where(hediff => hediff.def == HediffDefOf.PregnantHuman
                    || hediff.def == HediffDefOf.PregnancyLabor)
                .ToList();
            for (int i = 0; i < hediffs.Count; i++)
            {
                mother.health.RemoveHediff(hediffs[i]);
            }
        }

        //函数职责：从受精卵生成固定三岁凝砂儿童，应用母方蜕皮快照、建立父母关系并完成破壳清理。
        public static void HatchEgg(CompNingshaEmbryo embryo)
        {
            Thing egg = embryo?.parent;
            if (egg == null || egg.Destroyed || !embryo.IsInsideHatchNest)
            {
                Log.Error("[NingshaRace] 破壳失败：受精卵不存在或不在凝砂孵化巢内。");
                return;
            }

            Pawn mother = embryo.Mother;
            Pawn father = embryo.Father;
            Faction faction = embryo.HatcheeFaction ?? mother?.Faction ?? Faction.OfPlayer;
            PawnGenerationRequest request = new PawnGenerationRequest(
                DefOfRefs.NingshaRace_Child,
                faction,
                PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                allowPregnant: false,
                fixedBiologicalAge: 3f,
                fixedChronologicalAge: 3f,
                forcedXenotype: DefOfRefs.NingshaRace_Xenotype,
                developmentalStages: DevelopmentalStage.Child);
            Pawn child = PawnGenerator.GeneratePawn(request);
            child.TryGetComp<CompNingshaMolting>()?.ApplyInheritedCount(embryo.InheritedMoltingCount);

            if (mother != null)
            {
                child.relations.AddDirectRelation(PawnRelationDefOf.Parent, mother);
            }
            if (father != null)
            {
                child.relations.AddDirectRelation(PawnRelationDefOf.Parent, father);
            }

            bool spawned = PawnUtility.TrySpawnHatchedOrBornPawn(child, egg);
            if (!spawned)
            {
                Find.WorldPawns.PassToWorld(child, PawnDiscardDecideMode.Discard);
            }
            else if (mother?.playerSettings != null && child.playerSettings != null && mother.Map == child.Map)
            {
                child.playerSettings.AreaRestrictionInPawnCurrentMap = mother.playerSettings.AreaRestrictionInPawnCurrentMap;
            }

            egg.Destroy();
            if (spawned && faction == Faction.OfPlayer)
            {
                Find.LetterStack.ReceiveLetter(
                    "NingshaRace_HatchLetterLabel".Translate(),
                    "NingshaRace_HatchLetterText".Translate(child.LabelShort),
                    LetterDefOf.PositiveEvent,
                    child);
            }
        }
    }
}
