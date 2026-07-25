using System.Collections.Generic;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Buildings;
using NingshaRaceLib.DesertPit.Generation.Caves;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Landmarks;
using NingshaRaceLib.DesertPit.Generation.Steps;

namespace NingshaRaceLib.DesertPit.Generation.Utility
{
    //类职责：提供沙漠巨坑遗迹建筑的随机选择、类型判断和朝向选择。
    public static class DesertPitRelicUtility
    {
        //函数职责：按权重选择图腾或石棺遗迹建筑。
        public static ThingDef ChooseRelicDef(bool allowSarcophagus)
        {
            List<KeyValuePair<ThingDef, float>> pool = new List<KeyValuePair<ThingDef, float>>
            {
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitTotemStatueA"), 12f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitTotemStatueB"), 12f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitTotemStatueC"), 12f)
            };

            if (allowSarcophagus)
            {
                pool.Add(new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitSarcophagus"), 7f));
                pool.Add(new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitOpenSarcophagus"), 5f));
            }

            return pool.RandomElementByWeight((KeyValuePair<ThingDef, float> entry) => entry.Value).Key;
        }

        //函数职责：判断遗迹建筑是否属于占地更大的石棺。
        public static bool IsSarcophagus(ThingDef relicDef)
        {
            return relicDef.defName == "NingshaRace_DesertPitSarcophagus" || relicDef.defName == "NingshaRace_DesertPitOpenSarcophagus";
        }

        //函数职责：为可旋转遗迹随机选择朝向，不可旋转遗迹保持默认北向。
        public static Rot4 ChooseRotation(ThingDef relicDef)
        {
            return relicDef.rotatable ? Rot4.Random : Rot4.North;
        }
    }
}
