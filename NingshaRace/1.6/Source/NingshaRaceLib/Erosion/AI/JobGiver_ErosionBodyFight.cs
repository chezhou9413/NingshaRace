using RimWorld;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.Erosion.AI
{
    //类职责：让侵蚀体主动锁定敌对 Pawn，并优先自动施放可用攻击能力后再近战追击。
    public sealed class JobGiver_ErosionBodyFight : JobGiver_AIFightEnemies
    {
        //函数职责：把攻击目标限制为其他阵营的 Pawn，避免侵蚀体破坏无生命建筑或同阵营实体。
        protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            return target is Pawn targetPawn
                && targetPawn.Faction != pawn.Faction
                && base.ExtraTargetValidator(pawn, target);
        }

        //函数职责：搜索射程内全部可达的敌对 Pawn，不因目标缺乏战斗能力而跳过。
        protected override Thing FindAttackTarget(Pawn pawn)
        {
            TargetScanFlags flags = TargetScanFlags.NeedLOSToPawns
                | TargetScanFlags.NeedReachableIfCantHitFromMyPos
                | TargetScanFlags.NeedAutoTargetable;
            return (Thing)AttackTargetFinder.BestAttackTarget(
                pawn,
                flags,
                target => ExtraTargetValidator(pawn, target),
                0f,
                targetAcquireRadius,
                GetFlagPosition(pawn),
                GetFlagRadius(pawn),
                canBashDoors: false,
                canTakeTargetsCloserThanEffectiveMinRange: true,
                canBashFences: false,
                OnlyUseRangedSearch);
        }
    }
}
