using System;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

using NingshaRaceLib.AltarMissions.Core;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.AltarMissions.Generation
{
    //类职责：在主定居点四至十世界格生成指定祭坛任务Site并建立无超时成败信号。
    public sealed class QuestNode_Root_AltarMission : QuestNode
    {
        //字段职责：由QuestScriptDef指定当前根节点生成的三类任务之一。
        public AltarMissionType missionType;

        //函数职责：确认主定居点存在且四至十格内至少有一个可承载Site的通行世界格。
        protected override bool TestRunInt(Slate slate)
        {
            Verse.Map home = Find.AnyPlayerHomeMap;
            return home != null && TryFindMissionTile(home.Tile, out PlanetTile _);
        }

        //函数职责：创建原版Site、登记Quest标签、生成世界物体并添加精确成功和失败结算。
        protected override void RunInt()
        {
            Verse.Map home = Find.AnyPlayerHomeMap;
            if (home == null || !TryFindMissionTile(home.Tile, out PlanetTile tile))
            {
                throw new InvalidOperationException("智慧之蛇祭坛任务无法在主定居点四至十格内找到合法地点。");
            }
            Quest quest = QuestGen.quest;
            SitePartDef sitePartDef = SitePartForMission();
            WorldObjectDef worldObjectDef = WorldObjectForMission();
            Site site = SiteMaker.MakeSite(sitePartDef, tile, null, false,
                StorytellerUtility.DefaultSiteThreatPointsNow(), worldObjectDef);
            QuestUtility.AddQuestTag(ref site.questTags, QuestGenUtility.HardcodedTargetQuestTagWithQuestID("site"));
            QuestGen.slate.Set("site", site);
            quest.SpawnWorldObject(site);
            AddOutcomeParts(quest, site);
        }

        //函数职责：使用原版世界图洪泛距离寻找四至十格内通行、未占用且可建图的世界格。
        private static bool TryFindMissionTile(PlanetTile root, out PlanetTile result)
        {
            return TileFinder.TryFindPassableTileWithTraversalDistance(root, 4, 10, out result,
                tile => !Find.WorldObjects.AnyWorldObjectAt(tile) && TileFinder.IsValidTileForNewSettlement(tile),
                false, TileFinderMode.Random);
        }

        //函数职责：按任务类型取得生成地图附加内容的SitePartDef。
        private SitePartDef SitePartForMission()
        {
            if (missionType == AltarMissionType.SmallRuins) return DefOfRefs.NingshaRace_AltarSmallRuinsPart;
            if (missionType == AltarMissionType.AntNest) return DefOfRefs.NingshaRace_AltarAntNestPart;
            return DefOfRefs.NingshaRace_AltarRescuePart;
        }

        //函数职责：按任务类型和解救任务地表地下各半概率选择专用WorldObjectDef。
        private WorldObjectDef WorldObjectForMission()
        {
            if (missionType == AltarMissionType.SmallRuins) return DefOfRefs.NingshaRace_AltarSmallRuinsSite;
            if (missionType == AltarMissionType.AntNest) return DefOfRefs.NingshaRace_AltarAntNestSite;
            return Rand.Chance(0.5f) ? DefOfRefs.NingshaRace_AltarRescueSurfaceSite : DefOfRefs.NingshaRace_AltarRescueUndergroundSite;
        }

        //函数职责：把三类任务的目标信号连接到原版成功或失败Quest结算并处理地图异常移除。
        private void AddOutcomeParts(Quest quest, Site site)
        {
            string successSuffix = missionType == AltarMissionType.SmallRuins ? "site.NoActiveThreats"
                : missionType == AltarMissionType.AntNest ? "site.AntNestDestroyed" : "site.RescueSucceeded";
            string successSignal = QuestGenUtility.HardcodedSignalWithQuestID(successSuffix);
            quest.Letter(LetterDefOf.PositiveEvent, inSignal: successSignal, lookTargets: new object[] { site },
                label: "智慧之蛇的指引完成", text: SuccessText());
            quest.End(QuestEndOutcome.Success, inSignal: successSignal,
                signalListenMode: QuestPart.SignalListenMode.OngoingOnly);

            if (missionType == AltarMissionType.RescueKinsfolk)
            {
                string failedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.RescueFailed");
                quest.Letter(LetterDefOf.NegativeEvent, inSignal: failedSignal, lookTargets: new object[] { site },
                    label: "解救同胞失败", text: "所有待救的凝砂族都已死亡。智慧之蛇的指引失去了意义。");
                quest.End(QuestEndOutcome.Fail, inSignal: failedSignal,
                    signalListenMode: QuestPart.SignalListenMode.OngoingOnly);
            }
            string removedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
            quest.End(QuestEndOutcome.Fail, inSignal: removedSignal,
                signalListenMode: QuestPart.SignalListenMode.OngoingOnly);
        }

        //函数职责：按任务类型提供成功信件正文。
        private string SuccessText()
        {
            if (missionType == AltarMissionType.SmallRuins) return "遗迹中的主动威胁已经全部消失，战利品可由殖民者自行搬运。";
            if (missionType == AltarMissionType.AntNest) return "目标蚁穴已被摧毁，逃散成员不影响任务完成。";
            return "指定侵蚀体已经全部死亡，幸存同胞正式加入了殖民地。";
        }
    }
}
