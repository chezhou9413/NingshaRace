using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.Scenarios.Generation;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.DesertPit.Discovery.World
{
    //类职责：保存永久巨坑地点、来源地图尺寸和发现任务，并提供远行队进入交互。
    public class WorldObject_DesertPitDiscovery : MapParent
    {
        public IntVec3 surfaceSize;
        public Quest discoveryQuest;
        public bool discovered;
        public Building_DesertPitGate gate;

        //属性职责：由专用抵达动作同时处理首次生成和重复进入。
        protected override bool UseGenericEnterMapFloatMenuOption => false;

        //属性职责：保持永久探索地点的父对象与地下入口关联，不转换成定居点。
        public override AcceptanceReport CanBeSettled => "巨坑探索地点不能转换为定居点。";

        //允许从世界地图主动放弃整个巨坑地点。
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
            yield return DesertPitAbandonUtility.CreateCommand(this);
        }

        //未进入的地点没有地表地图，同样可以终止任务并移除标记。
        public override void Abandon(bool wasGravshipLaunch)
        {
            DesertPitAbandonUtility.RemoveUndergroundMaps(this);
            if (discoveryQuest != null && (discoveryQuest.State == QuestState.Ongoing
                || discoveryQuest.State == QuestState.NotYetAccepted))
                discoveryQuest.End(QuestEndOutcome.Fail);
            gate = null;
            if (HasMap) base.Abandon(wasGravshipLaunch);
            else Destroy();
        }

        //函数职责：保存地图尺寸、任务引用和抵达完成标记。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref surfaceSize, "surfaceSize");
            Scribe_References.Look(ref discoveryQuest, "discoveryQuest");
            Scribe_Values.Look(ref discovered, "discovered");
            Scribe_References.Look(ref gate, "gate");
        }

        //函数职责：在地表完成生成后安置唯一的可通达巨坑入口。
        public override void PostMapGenerate()
        {
            base.PostMapGenerate();
            gate = NingshaStartingGatePlacement.Spawn(Map, Map.Center);
        }

        //函数职责：添加支持尚未生成地图的远行队进入选项。
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(caravan))
                yield return option;
            foreach (FloatMenuOption option in CaravanArrivalActionUtility.GetFloatMenuOptions(
                () => CaravanArrivalAction_DesertPit.CanEnter(caravan, this),
                () => new CaravanArrivalAction_DesertPit(this), "前往沙漠巨坑", caravan, Tile, this))
                yield return option;
        }

        //函数职责：在远行队成功入图后完成寻找任务，保留地点及地图。
        public void NotifyEntered()
        {
            discovered = true;
            if (discoveryQuest != null && discoveryQuest.State == QuestState.Ongoing)
                discoveryQuest.End(QuestEndOutcome.Success);
        }
    }
}
