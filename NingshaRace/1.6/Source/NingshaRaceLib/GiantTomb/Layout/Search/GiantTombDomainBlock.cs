namespace NingshaRaceLib.GiantTomb.Layout
{
    //结构职责：记录某个房间造成的候选失效，供撤销该房间时精确恢复候选。
    internal readonly struct GiantTombDomainBlock
    {
        private readonly bool[] blocked;
        private readonly int index;

        //职责：登记一个候选位图中的失效位置。
        public GiantTombDomainBlock(bool[] blocked, int index)
        {
            this.blocked = blocked;
            this.index = index;
        }

        //职责：在阻挡房间退出搜索栈时恢复候选，不影响其他房间造成的失效。
        public void Restore()
        {
            blocked[index] = false;
        }
    }
}
