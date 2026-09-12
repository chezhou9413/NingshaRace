using NingshaRaceLib.DesertPit.AntColony.Core;
using NingshaRaceLib.DesertPit.AntColony.State;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //类职责：为工蚁任务选择和持续执行提供所属巢群的死亡区域避让判断。
    public partial class MapComponent_DesertPitAntColonies
    {
        //函数职责：只限制工蚁进入本巢死亡热点，兵蚁调查和其他阶级行为保持独立。
        public bool IsWorkerForageDangerous(Pawn pawn, IntVec3 cell)
        {
            if (pawn.TryGetComp<Comp_DesertPitAntMember>()?.Caste != AntCaste.Worker) return false;
            AntColonyState state;
            return TryGetColony(pawn, out state)
                && state.WorkerDanger.Contains(state, Settings, cell, Find.TickManager.TicksGame);
        }
    }
}
