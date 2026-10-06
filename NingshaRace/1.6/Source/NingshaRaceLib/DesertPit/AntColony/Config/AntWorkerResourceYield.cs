using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Config
{
    //一次挖掘的产物、数量和概率权重；空产物表示本次没有收获。
    public sealed class AntWorkerResourceYield
    {
        public ThingDef thingDef;
        public int count;
        public float weight;
    }
}
