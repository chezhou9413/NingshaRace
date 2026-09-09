using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Discovery.World
{
    //类职责：在远行队抵达永久巨坑地点时生成地表、安置成员并完成任务。
    public class CaravanArrivalAction_DesertPit : CaravanArrivalAction
    {
        private WorldObject_DesertPitDiscovery destination;
        public override string Label => "前往沙漠巨坑";
        public override string ReportString => "正在前往沙漠巨坑。";

        //函数职责：供存档反序列化创建抵达动作。
        public CaravanArrivalAction_DesertPit() { }

        //函数职责：绑定本次行程的永久巨坑地点。
        public CaravanArrivalAction_DesertPit(WorldObject_DesertPitDiscovery destination)
        {
            this.destination = destination;
        }

        //函数职责：确认目的地仍存在且允许玩家远行队进入。
        public static FloatMenuAcceptanceReport CanEnter(Caravan caravan, WorldObject_DesertPitDiscovery site)
        {
            return caravan.IsPlayerControlled && site != null && site.Spawned;
        }

        //函数职责：行程期间验证目的地引用和世界坐标仍然一致。
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
            return report && CanEnter(caravan, destination) && destination.Tile == destinationTile;
        }

        //函数职责：把首次地图生成放入原版长事件，已有地图直接进入。
        public override void Arrived(Caravan caravan)
        {
            if (!destination.HasMap)
                LongEventHandler.QueueLongEvent(() => Enter(caravan), "GeneratingMapForNewEncounter", false, null);
            else
                Enter(caravan);
        }

        //函数职责：复用或生成记录尺寸的地表，再从边缘安置远行队并完成发现任务。
        private void Enter(Caravan caravan)
        {
            Map map = destination.Map ?? GetOrGenerateMapUtility.GetOrGenerateMap(
                destination.Tile, destination.surfaceSize, null);
            if (destination.gate == null || !destination.gate.Spawned)
                throw new InvalidOperationException("巨坑探索地点缺少有效入口，远行队未进入，寻找任务未完成。");
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge,
                CaravanDropInventoryMode.DoNotDrop, draftColonists: false,
                extraCellValidator: cell => map.reachability.CanReach(cell, destination.gate,
                    PathEndMode.Touch, TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater)));
            destination.NotifyEntered();
        }

        //函数职责：保存行进中远行队的目的地引用。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref destination, "destination");
        }
    }
}
