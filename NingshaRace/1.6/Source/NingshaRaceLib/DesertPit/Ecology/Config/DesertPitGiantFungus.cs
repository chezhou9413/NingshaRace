using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Config
{
    //类职责：配置一种巨菇在标准地下地图中的独立数量和植株间距。
    public sealed class DesertPitGiantFungus
    {
        public ThingDef plant;
        //数量以二百格见方的地图为基准，初始生成按地图面积缩放。
        public IntRange countRange = new IntRange(12, 18);
        public float spacing = 4f;
    }
}
