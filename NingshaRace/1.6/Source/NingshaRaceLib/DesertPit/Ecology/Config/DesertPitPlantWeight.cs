using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Config
{
    //类职责：保存一种洞穴植物及其在初始生成和后续再生中的相对权重。
    public class DesertPitPlantWeight
    {
        public ThingDef plant;
        public float weight = 1f;
    }
}
