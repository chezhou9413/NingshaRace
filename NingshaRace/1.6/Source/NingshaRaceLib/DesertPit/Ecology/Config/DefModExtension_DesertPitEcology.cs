using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Config
{
    //类职责：集中配置普通菌群、巨菇配额、再生间隔、幼株成长率和栖息地搜索范围。
    public class DefModExtension_DesertPitEcology : DefModExtension
    {
        public int regrowthIntervalTicks = 15000;
        public FloatRange initialGrowthRange = new FloatRange(0.15f, 0.35f);
        public float habitatRadius = 6f;
        public int placementAttempts = 120;
        public List<DesertPitPlantWeight> plants = new List<DesertPitPlantWeight>();
        //巨菇独立分配初始数量，不参与普通食药菌的权重抽样。
        public List<DesertPitGiantFungus> giantFungi = new List<DesertPitGiantFungus>();

        //函数职责：报告巨菇引用、配额和间距配置错误，阻止同一物种重复登记生态目标。
        public override IEnumerable<string> ConfigErrors()
        {
            HashSet<ThingDef> registered = new HashSet<ThingDef>();
            foreach (DesertPitPlantWeight entry in plants) registered.Add(entry.plant);
            foreach (DesertPitGiantFungus fungus in giantFungi)
            {
                if (fungus == null || fungus.plant == null || fungus.plant.plant == null)
                {
                    yield return "地下巨菇配置必须引用有效植物。";
                    continue;
                }
                if (!registered.Add(fungus.plant))
                    yield return "地下巨菇不能重复登记或同时进入普通植物池：" + fungus.plant.defName;
                if (fungus.countRange.min < 0 || fungus.countRange.max < fungus.countRange.min)
                    yield return "地下巨菇数量范围无效：" + fungus.plant.defName;
                if (fungus.spacing <= 0f)
                    yield return "地下巨菇间距必须大于零：" + fungus.plant.defName;
            }
        }
    }
}
