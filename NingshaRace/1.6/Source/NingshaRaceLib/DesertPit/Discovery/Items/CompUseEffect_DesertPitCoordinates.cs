using System;
using System.Linq;
using NingshaRaceLib.DesertPit.Discovery.Defs;
using NingshaRaceLib.DesertPit.Discovery.Quests;
using NingshaRaceLib.DesertPit.Discovery.World;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace NingshaRaceLib.DesertPit.Discovery.Items
{
    //类职责：解读坐标地图，在合法坐标与任务创建成功后消耗物品。
    public class CompUseEffect_DesertPitCoordinates : CompUseEffect
    {
        //属性职责：从任务管理器读取真实任务状态，防止重复开启。
        public static bool HasOngoingQuest => Find.QuestManager.QuestsListForReading.Any(
            quest => quest.root == DesertPitDiscoveryDefOf.NingshaRace_FindDesertPit
                && (quest.State == QuestState.Ongoing || quest.State == QuestState.NotYetAccepted));

        //函数职责：限制玩家成员在有效地表坐标上使用物品，并拒绝同时开启多个寻找任务。
        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer || pawn.MapHeld == null)
                return "只有地图上的玩家成员可以解读坐标。";
            if (!pawn.MapHeld.Tile.Valid || pawn.MapHeld.Tile.LayerDef != PlanetLayerDefOf.Surface)
                return "此处没有可用的星球地表坐标。";
            if (HasOngoingQuest)
                return "已有进行中的寻找巨坑任务。";
            return true;
        }

        //函数职责：先确定坐标与任务结构，再注册永久地点和任务，最后消耗一张地图。
        public override void DoEffect(Pawn usedBy)
        {
            AcceptanceReport report = CanBeUsedBy(usedBy);
            if (!report.Accepted)
            {
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            Map source = usedBy.MapHeld;
            if (!DesertPitTileFinder.TryFind(source.Tile, out PlanetTile tile, out string reason))
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            WorldObject_DesertPitDiscovery destination = (WorldObject_DesertPitDiscovery)
                WorldObjectMaker.MakeWorldObject(DesertPitDiscoveryDefOf.NingshaRace_DesertPitDiscovery);
            destination.Tile = tile;
            destination.surfaceSize = source.Size;
            Slate slate = new Slate();
            slate.Set("desertPitDestination", destination);
            slate.Set("points", 0f);
            Quest quest = QuestGen.Generate(DesertPitDiscoveryDefOf.NingshaRace_FindDesertPit, slate);
            if (quest == null || !quest.PartsListForReading.OfType<QuestPart_FindDesertPit>()
                .Any(part => part.destination == destination))
                throw new InvalidOperationException("巨坑寻找任务生成失败，坐标地图未消耗。");
            destination.discoveryQuest = quest;
            Find.WorldObjects.Add(destination);
            Find.QuestManager.Add(quest);
            parent.SplitOff(1).Destroy();
            Find.LetterStack.ReceiveLetter("寻找巨坑", "巨坑坐标已标记在世界地图上。派出远行队前往该地点即可完成寻找。",
                LetterDefOf.PositiveEvent, destination, quest: quest);
        }
    }
}
