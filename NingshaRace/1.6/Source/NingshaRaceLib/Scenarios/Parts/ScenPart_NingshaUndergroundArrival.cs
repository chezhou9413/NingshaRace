using RimWorld;
using Verse;

using NingshaRaceLib.Scenarios.Generation;

namespace NingshaRaceLib.Scenarios.Parts
{
    //类职责：在地表地图彻底完成后建立地下家园，不通过空投流程投放开局成员。
    public sealed class ScenPart_NingshaUndergroundArrival : ScenPart
    {
        //函数职责：串行生成连接地表的巨坑并安置开局队伍，避免嵌套生成破坏原版临时地图数据。
        public override void PostGameStart()
        {
            NingshaDesertPitStartUtility.CreateHome(Find.CurrentMap);
        }
    }
}
