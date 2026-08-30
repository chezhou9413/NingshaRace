using System.Linq;
using RimWorld;
using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Altar.Discovery
{
    //类职责：在玩家首次进入新沙漠巨坑时提示无主智慧之蛇祭坛必须先被占用。
    public sealed class MapComponent_WisdomSerpentAltarDiscovery : MapComponent
    {
        //字段职责：防止同一张沙漠巨坑地图反复发送祭坛发现信。
        private bool discoveryLetterSent;

        //构造函数职责：把祭坛发现信状态绑定到当前地图。
        public MapComponent_WisdomSerpentAltarDiscovery(Map map) : base(map)
        {
        }

        //函数职责：每六十Tick等待自由殖民者进入沙漠巨坑，再发送一次带祭坛定位的中性信件。
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (discoveryLetterSent || map.generatorDef != DefOfRefs.NingshaRace_DesertPitMap
                || !map.IsHashIntervalTick(60) || !map.mapPawns.FreeColonistsSpawned.Any())
            {
                return;
            }
            Thing altar = map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_Altar)
                .FirstOrDefault(thing => thing.Faction != Faction.OfPlayer);
            if (altar == null)
            {
                return;
            }
            RevealAltarArea(altar.Position);
            Find.LetterStack.ReceiveLetter(
                "发现智慧之蛇祭坛",
                "殖民者在沙漠巨坑中发现了一座无主的智慧之蛇祭坛。祭坛在被殖民地占用前不会接收生肉供奉。请使用原版“占用”命令将其划归殖民地，然后再安排自动搬运或右键优先填充。",
                LetterDefOf.NeutralEvent,
                altar);
            discoveryLetterSent = true;
        }

        //函数职责：在发送发现信前揭开祭坛周围六格区域，使信件定位后能够直接看见目标。
        private void RevealAltarArea(IntVec3 center)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 6f, true))
            {
                if (cell.InBounds(map))
                {
                    map.fogGrid.Unfog(cell);
                }
            }
        }

        //函数职责：保存发现信是否已经发送，确保存读档后不重复提醒。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref discoveryLetterSent, "wisdomSerpentAltarDiscoveryLetterSent", false);
        }
    }
}
