using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Utility;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Resources
{
    //类职责：抽取普通散饰的基础类别配额，只把钟乳石数量翻倍。
    internal static class DesertPitDecorationQuota
    {
        //函数职责：保留骨骸和稀有晶体的基础抽样数量，为每个钟乳石样本添加同形态的第二份。
        public static Queue<ThingDef> Create()
        {
            int baseline = Rand.RangeInclusive(105, 155);
            int crystalLimit = Rand.RangeInclusive(1, 3);
            int crystals = 0;
            List<ThingDef> result = new List<ThingDef>();
            for (int i = 0; i < baseline; i++)
            {
                ThingDef def = DesertPitDecorationUtility.ChooseDecorationDef(crystals < crystalLimit);
                result.Add(def);
                if (DesertPitDecorationUtility.IsStalactite(def)) result.Add(def);
                if (DesertPitDecorationUtility.IsCrystal(def)) crystals++;
            }
            result.Shuffle();
            return new Queue<ThingDef>(result);
        }
    }
}
