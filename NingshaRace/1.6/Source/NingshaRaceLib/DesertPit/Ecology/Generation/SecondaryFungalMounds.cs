using System.Collections.Generic;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Generation.Config;
using NingshaRaceLib.DesertPit.Generation.Data;
using NingshaRaceLib.DesertPit.Generation.Topology;
using Verse;

namespace NingshaRaceLib.DesertPit.Ecology.Generation
{
    //类职责：在两处次要洞室的干地分散放置可选菌巢，不改变保底菌巢或单巢植物密度。
    internal static class SecondaryFungalMounds
    {
        //函数职责：各洞室独立抽取数量并逐个安置，空间不足只减少可选数量并汇总警告。
        public static void Generate(Map map, DesertPitLayoutData data, DefModExtension_DesertPitLayout settings)
        {
            List<IntVec3> existing = new List<IntVec3>();
            foreach (Thing mound in map.listerThings.ThingsOfDef(DefOfRefs.NingshaRace_FungalMound)) existing.Add(mound.Position);
            int missing = 0;
            foreach (DesertPitRoom room in data.SecondaryRooms)
            {
                int count = settings.extraMoundsPerRoom.RandomInRange;
                for (int i = 0; i < count; i++)
                {
                    IntVec3 cell = FindCell(map, data, room, existing, settings.moundSpacing);
                    if (!cell.IsValid) { missing += count - i; break; }
                    AntHabitatGeneration.SpawnMound(map, data, cell);
                    existing.Add(cell);
                }
            }
            if (missing > 0) Log.Warning("沙漠巨坑次要洞室干地不足，减少了 " + missing + " 处可选共生菌巢；保底菌巢不受影响。");
        }

        //函数职责：只在该洞室真实地面中选址，核对完整建筑占地、周边菌床和既有菌巢间距。
        private static IntVec3 FindCell(Map map, DesertPitLayoutData data, DesertPitRoom room, List<IntVec3> existing, float spacing)
        {
            IntVec3 best = IntVec3.Invalid;
            float bestScore = float.MinValue;
            foreach (IntVec3 cell in room.Floor)
            {
                if (!FarEnough(cell, existing, spacing) || !AntHabitatGeneration.CanPlace(map, data, cell, DefOfRefs.NingshaRace_FungalMound, false)) continue;
                bool inside = true;
                foreach (IntVec3 occupied in GenAdj.OccupiedRect(cell, Rot4.North, DefOfRefs.NingshaRace_FungalMound.size))
                    if (!room.Floor.Contains(occupied)) { inside = false; break; }
                if (!inside) continue;
                int growingCells = 0;
                foreach (IntVec3 nearby in GenRadial.RadialCellsAround(cell, 5f, true))
                    if (room.Floor.Contains(nearby) && AntHabitatGeneration.CanUseGround(map, data, nearby, false)) growingCells++;
                if (growingCells < 36) continue;
                float score = growingCells + Rand.Range(0f, 6f);
                if (score > bestScore) { best = cell; bestScore = score; }
            }
            return best;
        }

        //函数职责：以实际中心间距避免多个菌床靠拢成过密产地。
        private static bool FarEnough(IntVec3 cell, List<IntVec3> existing, float spacing)
        {
            foreach (IntVec3 position in existing)
                if (cell.DistanceToSquared(position) < spacing * spacing) return false;
            return true;
        }
    }
}
