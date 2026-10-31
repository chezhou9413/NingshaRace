using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Combat
{
    //冲锋对所有当前战斗目标使用同一距离与直线路径检查。
    internal static class AntChargeUtility
    {
        //允许近身敌人和建筑边缘作为终点，不穿越墙体或斜角。
        public static bool TryGetDestination(Pawn pawn, Thing target, float range, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            if (target == null || !target.Spawned || target.Map != pawn.Map || target.Destroyed
                || target is Pawn victim && (victim.Dead || victim.Downed)) return false;
            IntVec3 end = target.OccupiedRect().ClosestCellTo(pawn.Position);
            if (end == pawn.Position || end.DistanceToSquared(pawn.Position) > range * range) return false;
            IntVec3 previous = pawn.Position;
            foreach (IntVec3 cell in GenSight.BresenhamCellsBetween(pawn.Position, end))
            {
                if (cell == pawn.Position) continue;
                if (!CornersClear(pawn.Map, previous, cell)
                    || !CanEnter(pawn.Map, cell) && !(cell == end && IsTargetBuildingAt(target, cell))) return false;
                previous = cell;
            }
            destination = end;
            return true;
        }

        //两格之间的斜向移动也要检查两侧墙体。
        public static bool CornersClear(Map map, IntVec3 from, IntVec3 to) => to.InBounds(map)
            && (from.x == to.x || from.z == to.z
                || CanEnter(map, new IntVec3(to.x, 0, from.z)) && CanEnter(map, new IntVec3(from.x, 0, to.z)));

        //关闭的门也作为实体障碍，不沿用可开门寻路的通行判定。
        public static bool CanEnter(Map map, IntVec3 cell) => cell.InBounds(map) && cell.Standable(map)
            && !(cell.GetEdifice(map) is Building_Door door && !door.Open);

        //每步只检查前方及斜角两格，返回实际阻挡建筑用于撞击结算。
        public static bool TryGetObstacle(Map map, IntVec3 from, IntVec3 to, out IntVec3 cell, out Building building)
        {
            cell = to;
            if (CanEnter(map, to))
            {
                if (from.x == to.x || from.z == to.z) { building = null; return false; }
                cell = new IntVec3(to.x, 0, from.z);
                if (CanEnter(map, cell))
                {
                    cell = new IntVec3(from.x, 0, to.z);
                    if (CanEnter(map, cell)) { building = null; return false; }
                }
            }
            building = cell.InBounds(map) ? cell.GetEdifice(map) : null;
            return true;
        }

        //只有锁定的敌方建筑可以在路线末端承受撞击，普通障碍仍然挡住冲锋。
        public static bool IsTargetBuildingAt(Thing target, IntVec3 cell) => target is Building
            && target.Spawned && !target.Destroyed && target.OccupiedRect().Contains(cell);
    }
}
