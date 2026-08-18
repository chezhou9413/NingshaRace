using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit.Sarcophagus.Config
{
    //类职责：把封闭石棺ThingDef连接到内容配置、开启后建筑和开启动作耗时。
    public sealed class DefModExtension_NingshaSarcophagus : DefModExtension
    {
        public NingshaSarcophagusLootDef lootDef;
        public ThingDef openedThingDef;
        public int openTicks = 300;

        //函数职责：报告会阻止石棺初始化或替换的扩展配置错误。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (lootDef == null)
            {
                yield return "lootDef不能为空";
            }
            if (openedThingDef == null)
            {
                yield return "openedThingDef不能为空";
            }
            if (openTicks < 1)
            {
                yield return "openTicks必须大于零";
            }
        }
    }
}
