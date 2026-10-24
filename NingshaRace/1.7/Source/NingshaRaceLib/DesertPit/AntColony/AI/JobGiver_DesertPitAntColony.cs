using Verse;
using Verse.AI;

using NingshaRaceLib.DesertPit.AntColony.Components;

namespace NingshaRaceLib.DesertPit.AntColony.AI
{
    //类职责：把 ThinkTree 的工作请求交给当前地图唯一的蚁群管理组件决策。
    public class JobGiver_DesertPitAntColony : ThinkNode_JobGiver
    {
        //函数职责：取得指定蚂蚁当前最高优先级的巢群工作。
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Map == null)
            {
                return null;
            }

            return pawn.Map.GetComponent<MapComponent_DesertPitAntColonies>().TryCreateColonyJob(pawn);
        }
    }
}
