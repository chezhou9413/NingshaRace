namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：保存主线程预先建立的一次确定性布局尝试及其纯计算输入。
    internal sealed class GiantTombLayoutSearchAttempt
    {
        public readonly int Index;
        public readonly GiantTombModule[] Pool;
        public readonly int RandomSeed;
        public readonly int CandidateBudget;
        public readonly int BranchCount;
        public readonly int LeafCount;
        public readonly int TransitRoomCount;
        public readonly int CorridorCount;

        //函数职责：冻结一次后台求解需要的模块池、随机种子、预算和分类统计。
        public GiantTombLayoutSearchAttempt(int index, GiantTombModule[] pool, int randomSeed, int candidateBudget,
            int branchCount, int leafCount, int transitRoomCount, int corridorCount)
        {
            Index = index;
            Pool = pool;
            RandomSeed = randomSeed;
            CandidateBudget = candidateBudget;
            BranchCount = branchCount;
            LeafCount = leafCount;
            TransitRoomCount = transitRoomCount;
            CorridorCount = corridorCount;
        }
    }
}
