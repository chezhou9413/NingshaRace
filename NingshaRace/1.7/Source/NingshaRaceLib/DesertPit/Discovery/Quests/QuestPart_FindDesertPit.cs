using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Discovery.World;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.DesertPit.Discovery.Quests
{
    //类职责：为寻找巨坑任务提供地图跳转目标，并保存目标关联。
    public class QuestPart_FindDesertPit : QuestPart
    {
        public WorldObject_DesertPitDiscovery destination;
        public override string QuestSelectTargetsLabel => "查看巨坑坐标";

        //属性职责：向任务面板暴露永久世界地点。
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                if (destination != null && destination.Spawned)
                    yield return new GlobalTargetInfo(destination);
            }
        }

        //属性职责：使任务面板的查看按钮定位到同一世界地点。
        public override IEnumerable<GlobalTargetInfo> QuestSelectTargets => QuestLookTargets;

        //函数职责：保存任务与世界地点之间的引用。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref destination, "destination");
        }
    }
}
