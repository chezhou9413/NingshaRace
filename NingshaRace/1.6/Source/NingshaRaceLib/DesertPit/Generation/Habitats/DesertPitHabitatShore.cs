using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Generation.Data;
using Verse;

namespace NingshaRaceLib.DesertPit.Generation.Habitats
{
    //类职责：为水潭植被和天光选点提供相同的三格岸带范围。
    internal static class DesertPitHabitatShore
    {
        //函数职责：收集距水面三格以内的生态干地，排除水面和通行核心。
        public static List<IntVec3> Collect(DesertPitHabitat habitat, DesertPitLayoutData data)
        {
            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in habitat.floor)
            {
                if (habitat.water.Contains(cell) || data.ProtectedRouteCells.Contains(cell)) continue;
                foreach (IntVec3 near in GenRadial.RadialCellsAround(cell, 3f, true))
                    if (habitat.water.Contains(near)) { result.Add(cell); break; }
            }
            return result;
        }
    }
}
