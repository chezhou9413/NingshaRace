using Verse;

namespace NingshaRaceLib.Combat.Utility
{
    //筛选自定义武器可正常结算伤害的目标。
    internal static class NingshaDamageTargetUtility
    {
        //保留对各阵营的强制攻击，排除血肉心脏等不能直接攻击的建筑。
        public static bool IsValid(Pawn attacker, Thing target)
        {
            if (attacker == null || target == null || target == attacker
                || !target.Spawned || target.Destroyed || target.Map != attacker.Map)
                return false;

            if (target is Pawn pawn) return !pawn.Dead;
            return target.def.category == ThingCategory.Building && target.def.destroyable
                && target.def.useHitPoints && target.def.building.isTargetable;
        }
    }
}
