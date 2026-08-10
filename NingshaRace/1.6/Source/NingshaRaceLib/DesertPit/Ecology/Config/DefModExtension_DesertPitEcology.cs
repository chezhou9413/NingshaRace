using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Config
{
    //类职责：集中配置沙漠巨坑菌群的植物池、再生间隔、幼株成长率和栖息地搜索范围。
    public class DefModExtension_DesertPitEcology : DefModExtension
    {
        public int regrowthIntervalTicks = 15000;
        public FloatRange initialGrowthRange = new FloatRange(0.15f, 0.35f);
        public float habitatRadius = 6f;
        public int placementAttempts = 120;
        public List<DesertPitPlantWeight> plants = new List<DesertPitPlantWeight>();
    }
}
