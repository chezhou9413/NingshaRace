using NingshaRaceLib.Petrification.Utility;
using RimWorld;
using Verse;

namespace NingshaRaceLib.GiantTomb.Pawns.Combat
{
    //类职责：执行木乃伊原版毒性抓伤，并为仍存活的血肉目标累计可配置石化进度。
    public sealed class Verb_GiantTombMummyClaw : Verb_MeleeAttackDamage
    {
        //属性职责：取得当前近战动作的木乃伊利爪专属参数。
        private VerbProperties_GiantTombMummyClaw MummyClawProps =>
            (VerbProperties_GiantTombMummyClaw)verbProps;

        //函数职责：先完成原版护甲、伤害和毒素结算，再按实际有效命中增加石化严重度。
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult result = base.ApplyMeleeDamageToTarget(target);
            Pawn targetPawn = target.Pawn;
            if (result.totalDamageDealt > 0f && targetPawn != null && !targetPawn.Dead && targetPawn.RaceProps.IsFlesh)
            {
                PetrificationUtility.AddSeverity(targetPawn, MummyClawProps.petrificationSeverity);
            }
            return result;
        }
    }
}
