using System;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Erosion.Health.Components;
using NingshaRaceLib.Erosion.Utility;
using NingshaRaceLib.Storyteller.Quests.Parts;

namespace NingshaRaceLib.Storyteller.Quests.Generation
{
    //类职责：生成凝砂族加入者、追杀侵蚀体、延迟入场和明确成败条件组成的一次性任务。
    public sealed class QuestNode_Root_NingshaErosionPursuit : QuestNode
    {
        private static readonly IntRange JoinerDelayTicks = new IntRange(600, 1200);
        private static readonly IntRange ErosionBodyDelayTicks = new IntRange(1800, 2400);

        //函数职责：确认当前世界具有可承载任务的玩家主地图、自由殖民者和实体阵营。
        protected override bool TestRunInt(Slate slate)
        {
            Map map = Find.AnyPlayerHomeMap;
            return map != null
                && Faction.OfEntities != null
                && PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended.Any()
                && RCellFinder.TryFindRandomPawnEntryCell(
                    out IntVec3 _,
                    map,
                    CellFinder.EdgeRoadChance_Neutral);
        }

        //函数职责：创建任务 Pawn、共享入场点、追杀关系、入场延迟以及所有任务结算部件。
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                throw new InvalidOperationException("侵蚀追杀任务生成时没有可用的玩家主地图。");
            }
            if (!RCellFinder.TryFindRandomPawnEntryCell(
                    out IntVec3 walkInSpot,
                    map,
                    CellFinder.EdgeRoadChance_Neutral))
            {
                throw new InvalidOperationException("侵蚀追杀任务无法找到有效的地图边缘入场点。");
            }

            Pawn joiner = GenerateJoiner(map);
            Pawn erosionBody = ErosionBodySpawnUtility.Generate(
                DefOfRefs.NingshaRace_Colonist,
                Faction.OfEntities,
                map.Tile);
            ConfigurePursuitTarget(erosionBody, joiner);
            PassPawnToWorld(joiner);
            PassPawnToWorld(erosionBody);
            RegisterQuestData(slate, map, walkInSpot, joiner, erosionBody);

            quest.AddInvolvedFaction(Faction.OfEntities);
            AddArrivalParts(quest, map, walkInSpot, joiner, erosionBody);
            AddOutcomeParts(quest, map, joiner, erosionBody);
        }

        //函数职责：生成一名成年、可招募并允许建立殖民者关系的凝砂族加入者。
        private static Pawn GenerateJoiner(Map map)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                DefOfRefs.NingshaRace_Colonist,
                null,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: true,
                mustBeCapableOfViolence: false,
                allowPregnant: false,
                forceRecruitable: true);
            Pawn joiner = PawnGenerator.GeneratePawn(request);
            joiner.relations.everSeenByPlayer = true;
            return joiner;
        }

        //函数职责：把凝砂族加入者写入侵蚀体 Hediff 的持久化优先追杀目标。
        private static void ConfigurePursuitTarget(Pawn erosionBody, Pawn joiner)
        {
            Hediff erosionHediff = erosionBody.health.hediffSet
                .GetFirstHediffOfDef(DefOfRefs.NingshaRace_ErosionBody);
            HediffComp_ErosionPursuitTarget pursuitComp =
                erosionHediff?.TryGetComp<HediffComp_ErosionPursuitTarget>();
            if (pursuitComp == null)
            {
                throw new InvalidOperationException("侵蚀体缺少追杀目标 HediffComp。");
            }
            pursuitComp.SetPursuitTarget(joiner);
        }

        //函数职责：把任务生成 Pawn 放入世界 Pawn 池并登记为当前任务生成对象。
        private static void PassPawnToWorld(Pawn pawn)
        {
            if (!pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn);
            }
            QuestGen.AddToGeneratedPawns(pawn);
        }

        //函数职责：注册任务文本变量和精确 Pawn、地图信号标签。
        private static void RegisterQuestData(
            Slate slate,
            Map map,
            IntVec3 walkInSpot,
            Pawn joiner,
            Pawn erosionBody)
        {
            slate.Set("map", map);
            slate.Set("walkInSpot", walkInSpot);
            slate.Set("joiner", joiner);
            slate.Set("erosionBody", erosionBody);

            QuestUtility.AddQuestTag(
                ref joiner.questTags,
                QuestGenUtility.HardcodedTargetQuestTagWithQuestID("joiner"));
            QuestUtility.AddQuestTag(
                ref erosionBody.questTags,
                QuestGenUtility.HardcodedTargetQuestTagWithQuestID("erosionBody"));
            QuestUtility.AddQuestTag(
                ref map.Parent.questTags,
                QuestGenUtility.HardcodedTargetQuestTagWithQuestID("map"));
        }

        //函数职责：让加入者和追杀侵蚀体按原版追杀任务节奏从同一地图边缘依次进入。
        private static void AddArrivalParts(
            Quest quest,
            Map map,
            IntVec3 walkInSpot,
            Pawn joiner,
            Pawn erosionBody)
        {
            quest.Delay(
                JoinerDelayTicks.RandomInRange,
                delegate
                {
                    quest.PawnsArrive(
                        Gen.YieldSingle(joiner),
                        mapParent: map.Parent,
                        arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn,
                        joinPlayer: true,
                        walkInSpot: walkInSpot,
                        customLetterLabel: "凝砂族抵达",
                        customLetterText: joiner.LabelShortCap + "已经抵达殖民地，并永久加入了你。",
                        sendStandardLetter: true);
                },
                debugLabel: "凝砂族加入者入场延迟");

            quest.Delay(
                ErosionBodyDelayTicks.RandomInRange,
                delegate
                {
                    quest.PawnsArrive(
                        Gen.YieldSingle(erosionBody),
                        mapParent: map.Parent,
                        arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn,
                        joinPlayer: false,
                        walkInSpot: walkInSpot,
                        customLetterLabel: "侵蚀体来袭",
                        customLetterText: "追杀" + joiner.LabelShortCap + "的侵蚀体已经抵达。杀死或收容它才能结束追杀。",
                        sendStandardLetter: true);
                },
                debugLabel: "追杀侵蚀体入场延迟");
        }

        //函数职责：建立侵蚀体死亡或收容成功、加入者先死亡失败和任务地图移除失败的结算信号。
        private static void AddOutcomeParts(Quest quest, Map map, Pawn joiner, Pawn erosionBody)
        {
            string joinerKilledSignal = QuestGenUtility.HardcodedSignalWithQuestID("joiner.Killed");
            string erosionKilledSignal = QuestGenUtility.HardcodedSignalWithQuestID("erosionBody.Killed");
            string mapRemovedSignal = QuestGenUtility.HardcodedSignalWithQuestID("map.MapRemoved");
            string erosionContainedSignal = QuestGen.GenerateNewSignal("ErosionBodyContained");

            QuestPart_ErosionBodyContained containmentPart = new QuestPart_ErosionBodyContained
            {
                erosionBody = erosionBody,
                inSignalEnable = quest.InitiateSignal,
                signalListenMode = QuestPart.SignalListenMode.OngoingOnly,
                debugLabel = "监视指定侵蚀体收容状态"
            };
            containmentPart.outSignalsCompleted.Add(erosionContainedSignal);
            quest.AddPart(containmentPart);

            AddSuccessOutcome(quest, erosionBody, erosionKilledSignal, "追杀侵蚀体已被消灭");
            AddSuccessOutcome(quest, erosionBody, erosionContainedSignal, "追杀侵蚀体已被收容");

            quest.Letter(
                LetterDefOf.NegativeEvent,
                inSignal: joinerKilledSignal,
                lookTargets: new object[] { joiner },
                label: "凝砂族追杀失败",
                text: joiner.LabelShortCap + "在侵蚀体被解决前死亡了，任务失败。");
            quest.End(
                QuestEndOutcome.Fail,
                inSignal: joinerKilledSignal,
                signalListenMode: QuestPart.SignalListenMode.OngoingOnly);

            quest.Letter(
                LetterDefOf.NegativeEvent,
                inSignal: mapRemovedSignal,
                label: "凝砂族追杀失败",
                text: map.Parent.LabelCap + "已经不再可用，追杀任务无法继续。");
            quest.End(
                QuestEndOutcome.Fail,
                inSignal: mapRemovedSignal,
                signalListenMode: QuestPart.SignalListenMode.OngoingOnly);
        }

        //函数职责：为侵蚀体死亡或收容信号添加对应成功信件与任务成功结算。
        private static void AddSuccessOutcome(
            Quest quest,
            Pawn erosionBody,
            string successSignal,
            string successText)
        {
            quest.Letter(
                LetterDefOf.PositiveEvent,
                inSignal: successSignal,
                lookTargets: new object[] { erosionBody },
                label: "凝砂族追杀结束",
                text: successText + "。那名凝砂族终于摆脱了追猎者。");
            quest.End(
                QuestEndOutcome.Success,
                inSignal: successSignal,
                signalListenMode: QuestPart.SignalListenMode.OngoingOnly);
        }
    }
}
