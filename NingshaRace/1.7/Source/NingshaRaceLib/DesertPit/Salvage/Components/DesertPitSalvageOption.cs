using Verse;

namespace NingshaRaceLib.DesertPit.Salvage.Components
{
    //类职责：描述洞穴物件拆除时一种互斥资源结果的定义、数量和抽取权重。
    public sealed class DesertPitSalvageOption
    {
        //字段职责：指定该拆除结果生成的资源定义。
        public ThingDef thingDef;

        //字段职责：指定该拆除结果生成的资源数量。
        public int count;

        //字段职责：指定该拆除结果参与互斥抽取的权重。
        public float weight;
    }
}
