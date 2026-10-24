using NingshaRaceLib.DesertPit.Antlion.Components;
using NingshaRaceLib.DesertPit.Antlion.Holders;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Antlion.AI
{
    //类职责：寻找可步行到达且没有实体占据的沙地，禁止在地板或建筑内潜伏。
    internal static class AntlionSandUtility
    {
        //函数职责：检查出土格不覆盖 Pawn、建筑或其他地下实体，地面掉落物不妨碍站立。
        public static bool CanSurfaceAt(Map map, IntVec3 cell, Thing occupant)
        {
            if (!cell.InBounds(map) || !cell.Standable(map)) return false;
            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
                if (things[i] != occupant && (things[i] is Pawn || things[i] is Building || things[i] is AntlionBurrow))
                    return false;
            return true;
        }

        //下潜时检查全部占用，已潜伏时允许生物踩过而不强制释放蚁狮。
        public static bool CanBurrowAt(Map map, IntVec3 cell, CompProperties_AntlionAmbush props,
            Thing occupant = null, bool ignorePawns = false)
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || !props.burrowTerrains.Contains(cell.GetTerrain(map)))
                return false;
            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == occupant) continue;
                if ((thing is Pawn && !ignorePawns) || thing is Building || thing is AntlionBurrow
                    || thing is Plant || thing.def.category == ThingCategory.Item) return false;
            }
            return true;
        }

        //函数职责：按最近距离搜索有限半径的沙地，只接受无需破门或穿水的实际可达格。
        public static IntVec3 FindNearest(Pawn pawn, CompProperties_AntlionAmbush props)
        {
            int count = GenRadial.NumCellsInRadius(props.sandSearchRadius);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = pawn.Position + GenRadial.RadialPattern[i];
                if (CanBurrowAt(pawn.Map, cell, props, pawn)
                    && AntlionTargetUtility.CanReach(pawn.Map, pawn.Position, cell, PathEndMode.OnCell)) return cell;
            }
            return IntVec3.Invalid;
        }
    }
}
