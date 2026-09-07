using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Combat
{
    //类职责：按原版当前可用攻击及目标权重判断蚂蚁是否具备近战能力，不触发失败选招日志。
    internal static class AntMeleeCapability
    {
        //函数职责：同步检查当前身体、装备和健康提供的近战招式，不缓存原版共享的临时列表。
        public static bool CanAttack(Pawn pawn, Thing target)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || !pawn.kindDef.canMeleeAttack
                || target == null || !target.Spawned || target.Map != pawn.Map) return false;
            //原版列表会被下一次查询复用，必须在本函数内消费完，不跨线程或帧保存。
            var entries = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(false);
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].GetSelectionWeight(target) > 0f) return true;
            return false;
        }
    }
}
