using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.Discovery.Defs
{
    //类职责：提供巨坑坐标物品、任务和世界地点的定义引用。
    [DefOf]
    public static class DesertPitDiscoveryDefOf
    {
        public static ThingDef NingshaRace_DesertPitCoordinateMap;
        public static QuestScriptDef NingshaRace_FindDesertPit;
        public static WorldObjectDef NingshaRace_DesertPitDiscovery;

        //函数职责：确保首次访问前完成定义绑定。
        static DesertPitDiscoveryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DesertPitDiscoveryDefOf));
        }
    }
}
