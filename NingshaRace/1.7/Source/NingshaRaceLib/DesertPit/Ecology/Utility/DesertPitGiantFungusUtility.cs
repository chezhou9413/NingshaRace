using NingshaRaceLib.DesertPit.Ecology.Config;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Utility
{
    //类职责：为巨菇的初始散布和自然补生提供相同的生长空间约束。
    internal static class DesertPitGiantFungusUtility
    {
        //函数职责：根据植物定义取得独立巨菇配置，普通菌类返回空值。
        public static DesertPitGiantFungus Find(DefModExtension_DesertPitEcology settings, ThingDef plantDef)
        {
            foreach (DesertPitGiantFungus fungus in settings.giantFungi)
                if (fungus.plant == plantDef) return fungus;
            return null;
        }

        //函数职责：要求周围八格可以通行，并与现有树木及巨菇保持配置间距。
        public static bool HasSpace(Map map, IntVec3 cell, DesertPitGiantFungus fungus)
        {
            foreach (IntVec3 offset in GenAdj.AdjacentCells)
            {
                IntVec3 adjacent = cell + offset;
                if (!adjacent.InBounds(map) || !adjacent.Standable(map) || adjacent.GetEdifice(map) != null)
                    return false;
            }

            int radialCount = GenRadial.NumCellsInRadius(fungus.spacing);
            for (int i = 1; i < radialCount; i++)
            {
                IntVec3 nearby = cell + GenRadial.RadialPattern[i];
                if (!nearby.InBounds(map)) continue;
                var plant = nearby.GetPlant(map);
                if (plant != null && plant.def.plant.IsTree) return false;
            }
            return true;
        }
    }
}
