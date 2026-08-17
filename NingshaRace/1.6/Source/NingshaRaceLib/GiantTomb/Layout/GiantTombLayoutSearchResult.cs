using System.Collections.Generic;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：把后台布局搜索的完整结果和诊断统计一次性交还主线程。
    internal sealed class GiantTombLayoutSearchResult
    {
        public bool Success;
        public GiantTombLayoutSearchAttempt Attempt;
        public List<GiantTombPlacement> Placements;
        public List<GiantTombConnection> Connections;
        public long TotalEvaluations;
        public int DeepestPlacementCount;
        public long ElapsedMilliseconds;
    }
}
