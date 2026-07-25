using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Health
{
    //类职责：维护沙傀无需求、无关系和隐藏状态的标记行为。
    public class HediffComp_SandGolemMarker : HediffComp
    {
        //字段职责：记录沙傀累计承受的伤害量。
        private float absorbedDamage;

        //字段职责：控制沙傀可承受的总伤害阈值。
        private const float DamageLimit = 35f;

        //函数职责：保存沙傀累计伤害。
        public override void CompExposeData()
        {
            Scribe_Values.Look(ref absorbedDamage, "absorbedDamage");
        }

        //函数职责：隐藏健康面板里的沙傀标记状态。
        public override bool CompDisallowVisible()
        {
            return true;
        }

        //函数职责：沙傀状态加入后立即清理不需要的需求和关系。
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            SandGolemIdentityCleaner.Clean(Pawn);
            Pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            SandGolemUtility.StripNeedsAndRelations(Pawn);
            SandGolemUtility.EnsurePlayerControlComponents(Pawn);
        }

        //函数职责：沙傀被生成到地图后确保玩家控制组件存在。
        public override void Notify_Spawned()
        {
            SandGolemIdentityCleaner.Clean(Pawn);
            Pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            SandGolemUtility.StripNeedsAndRelations(Pawn);
            SandGolemUtility.EnsurePlayerControlComponents(Pawn);
        }

        //函数职责：周期性清理 RimWorld 动态组件可能补回的需求和关系。
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            SandGolemUtility.MaintainIdentity(Pawn, Find.TickManager.TicksGame);
            if (!SandGolemUtility.IsMovementLockedSandGolem(Pawn) && Pawn?.pather?.debugDisabled == true)
            {
                SandGolemUtility.RestoreControlAfterMovementLock(Pawn);
            }
        }

        //函数职责：累计沙傀受到的伤害，达到阈值后进入消散。
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            absorbedDamage += totalDamageDealt;
            if (absorbedDamage >= DamageLimit)
            {
                GameComponent_SandGolemTracker.Current?.BeginDissolve(Pawn, destroyPawn: true);
            }
        }

        //函数职责：沙傀死亡时启动消散记录。
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            GameComponent_SandGolemTracker.Current?.BeginDissolve(Pawn, destroyPawn: true);
        }
    }
}
