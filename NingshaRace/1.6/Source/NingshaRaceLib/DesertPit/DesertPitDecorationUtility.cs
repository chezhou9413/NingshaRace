using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.DesertPit
{
    //类职责：提供沙漠巨坑洞穴装饰物的类型判断和随机权重池。
    public static class DesertPitDecorationUtility
    {
        //函数职责：按权重选择钟乳石、骨骸或发光水晶装饰物。
        public static ThingDef ChooseDecorationDef(bool allowCrystal)
        {
            List<KeyValuePair<ThingDef, float>> pool = new List<KeyValuePair<ThingDef, float>>
            {
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteA"), 20f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteB"), 18f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteC"), 20f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteD"), 20f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteE"), 16f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteF"), 18f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitBonesA"), 8f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitBonesB"), 8f)
            };

            if (allowCrystal)
            {
                pool.Add(new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlowCrystal"), 6f));
            }

            return pool.RandomElementByWeight((KeyValuePair<ThingDef, float> entry) => entry.Value).Key;
        }

        //函数职责：按水晶地貌的视觉比例选择不同尺寸的发光砂晶。
        public static ThingDef ChooseCrystalDef()
        {
            List<KeyValuePair<ThingDef, float>> pool = new List<KeyValuePair<ThingDef, float>>
            {
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlowCrystalShard"), 18f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlowCrystal"), 12f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlowCrystalBloom"), 10f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitGlowCrystalPillar"), 6f)
            };

            return pool.RandomElementByWeight((KeyValuePair<ThingDef, float> entry) => entry.Value).Key;
        }

        //函数职责：按石林地貌的视觉比例选择不同形态的钟乳石。
        public static ThingDef ChooseStalactiteDef()
        {
            List<KeyValuePair<ThingDef, float>> pool = new List<KeyValuePair<ThingDef, float>>
            {
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteA"), 20f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteB"), 18f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteC"), 20f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteD"), 20f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteE"), 14f),
                new KeyValuePair<ThingDef, float>(DefDatabase<ThingDef>.GetNamed("NingshaRace_DesertPitStalactiteF"), 22f)
            };

            return pool.RandomElementByWeight((KeyValuePair<ThingDef, float> entry) => entry.Value).Key;
        }

        //函数职责：判断装饰物是否属于需要更大间距的大型摆件。
        public static bool IsLargeDecoration(ThingDef decorationDef)
        {
            return decorationDef.defName == "NingshaRace_DesertPitStalactiteE" || IsCrystal(decorationDef);
        }

        //函数职责：判断装饰物是否属于钟乳石类。
        public static bool IsStalactite(ThingDef decorationDef)
        {
            return decorationDef.defName.StartsWith("NingshaRace_DesertPitStalactite");
        }

        //函数职责：判断装饰物是否属于骨骸类。
        public static bool IsBones(ThingDef decorationDef)
        {
            return decorationDef.defName.StartsWith("NingshaRace_DesertPitBones");
        }

        //函数职责：判断装饰物是否属于发光水晶。
        public static bool IsCrystal(ThingDef decorationDef)
        {
            return decorationDef.defName.StartsWith("NingshaRace_DesertPitGlowCrystal");
        }
    }
}
