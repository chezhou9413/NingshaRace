using NingshaRaceLib.SandGolem.Utility;
using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.SandGolem.Automation
{
    //类职责：仅在召唤者的居住区和允许活动范围内寻找可到达的沙地。
    internal static class SandGolemAutoSummonTarget
    {
        //函数职责：校验居住区地形，允许把临时被角色占用的沙地保留为候选。
        public static bool IsHomeSand(Pawn pawn, IntVec3 cell, bool requireFree)
        {
            return pawn.Spawned && cell.InBounds(pawn.Map) && pawn.Map.areaManager.Home[cell]
                && !cell.IsForbidden(pawn)
                && SandGolemUtility.IsValidSandCell(cell, pawn.Map, out _, requireFree);
        }

        //函数职责：从居住区中寻找最近的可达沙地，不扫描整张地图。
        public static bool TryFind(Pawn pawn, bool requireFree, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            float best = float.MaxValue;
            foreach (IntVec3 cell in pawn.Map.areaManager.Home.ActiveCells)
            {
                float distance = cell.DistanceToSquared(pawn.Position);
                if (distance >= best || !IsHomeSand(pawn, cell, requireFree)
                    || !pawn.CanReach(cell, PathEndMode.Touch, Danger.Some)) continue;
                if (requireFree && !pawn.CanReserve(cell)) continue;
                result = cell;
                best = distance;
            }
            return result.IsValid;
        }
    }
}
