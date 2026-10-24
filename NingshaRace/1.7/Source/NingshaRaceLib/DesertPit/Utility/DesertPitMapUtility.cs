using Verse;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.DesertPit.Utility
{
    //类职责：统一识别普通口袋巨坑与凝砂族场景使用的地下巨坑主地图。
    public static class DesertPitMapUtility
    {
        //函数职责：判断地图是否由任一种沙漠巨坑地图生成器创建。
        public static bool IsDesertPitMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            MapGeneratorDef generator = map.generatorDef;
            return generator == DefOfRefs.NingshaRace_DesertPitMap
                || generator == DefOfRefs.NingshaRace_DesertPitStartingMap;
        }
    }
}
