using NingshaRaceLib.Core.Defs;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Genes.Conditions
{
    //类职责：在夜间或指定沙漠生态中启用沙漠适应基因配置的条件属性。
    public sealed class ConditionalStatAffecter_NingshaDesertAdaptation : ConditionalStatAffecter
    {
        //属性职责：返回属性面板中用于解释条件加成的中文标签。
        public override string Label => "夜间或沙漠环境";

        //函数职责：判断属性请求对应的 Pawn 是否处于夜间或受认可的沙漠生态。
        public override bool Applies(StatRequest req)
        {
            if (!req.HasThing || !(req.Thing is Pawn pawn) || !pawn.Spawned)
            {
                return false;
            }

            Map map = pawn.Map;
            return IsNight(map) || IsDesertBiome(map.Biome);
        }

        //函数职责：使用原版天体光照阈值判断地图当前是否不属于白昼。
        private static bool IsNight(Map map)
        {
            return !GenCelestial.IsDaytime(GenCelestial.CurCelestialSunGlow(map));
        }

        //函数职责：判断地图生态是否为原版沙漠、极端沙漠或凝砂族沙漠巨坑。
        private static bool IsDesertBiome(BiomeDef biome)
        {
            return biome == BiomeDefOf.Desert
                || biome == DefOfRefs.ExtremeDesert
                || biome == DefOfRefs.NingshaRace_DesertPitBiome;
        }
    }
}
