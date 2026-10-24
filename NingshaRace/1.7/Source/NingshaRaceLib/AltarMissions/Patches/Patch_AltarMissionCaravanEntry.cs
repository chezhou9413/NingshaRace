using System;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

using NingshaRaceLib.AltarMissions.Map;

namespace NingshaRaceLib.AltarMissions.Patches
{
    //类职责：把地下祭坛任务的商队入场从原版地图边缘重定向到主洞室安全区。
    [HarmonyPatch(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[]
    {
        typeof(Caravan), typeof(Verse.Map), typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode),
        typeof(bool), typeof(Predicate<IntVec3>)
    })]
    public static class Patch_AltarMissionCaravanEntry
    {
        //函数职责：在地图登记了地下安全入场格时使用精确生成函数并跳过原版边缘选点。
        public static bool Prefix(Caravan caravan, Verse.Map map, CaravanDropInventoryMode dropInventoryMode, bool draftColonists)
        {
            MissionMapComponent component = map.GetComponent<MissionMapComponent>();
            if (!component.TryGetUndergroundEntryCell(out IntVec3 entryCell))
            {
                return true;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(entryCell, 7f, true))
            {
                if (cell.InBounds(map))
                {
                    map.fogGrid.Unfog(cell);
                }
            }

            CaravanEnterMapUtility.Enter(caravan, map,
                pawn => CellFinder.RandomSpawnCellForPawnNear(entryCell, map),
                dropInventoryMode, draftColonists);
            return false;
        }
    }
}
