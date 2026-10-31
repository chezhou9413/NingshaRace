using NingshaRaceLib.DesertPit.AntColony.Core;
using RimWorld;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Components
{
    //成员只保存归属和个人冷却；生产工作由地图巢群任务管理。
    public class Comp_DesertPitAntMember : ThingComp
    {
        private int colonyId;
        private int completedHaulsThisCycle;
        private int nextForageTick;
        public int NextPassageCheckTick;
        public int NextResourceDigTick;
        public int NextAbilityTick;
        public int ColonyId => colonyId;
        public AntCaste Caste => ((CompProperties_DesertPitAntMember)props).caste;

        //仅初次登记安排半天发掘计时，出巢不重置个人冷却。
        public void AssignColony(int id)
        {
            colonyId = id;
            if (Caste == AntCaste.Worker && NextResourceDigTick == 0) NextResourceDigTick = Find.TickManager.TicksGame + 30000;
        }

        //五次搬运后休整半天，免费接续搬运由任务绕过额度。
        public bool CanStartForage(int now, int limit)
        {
            if (Caste != AntCaste.Worker || now < nextForageTick) return false;
            if (nextForageTick > 0) { nextForageTick = 0; completedHaulsThisCycle = 0; }
            return completedHaulsThisCycle < limit;
        }

        //只在实物送达后累计次数。
        public void NotifySuccessfulHaul(int now, int limit, int cooldown)
        {
            if (++completedHaulsThisCycle >= limit) nextForageTick = now + cooldown;
        }

        //保存所有影响行为的个人计时。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref colonyId, "colonyId");
            Scribe_Values.Look(ref completedHaulsThisCycle, "completedHaulsThisCycle");
            Scribe_Values.Look(ref nextForageTick, "nextForageTick");
            Scribe_Values.Look(ref NextResourceDigTick, "nextResourceDigTick");
            Scribe_Values.Look(ref NextAbilityTick, "nextAbilityTick");
        }

        //生成和出巢共用成员登记。
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (colonyId > 0) parent.Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyMemberSpawned((Pawn)parent, colonyId);
        }

        //非致命伤害交给地图组件处理警报和有限反击。
        public override void PostPostApplyDamage(DamageInfo info, float totalDamageDealt)
        {
            base.PostPostApplyDamage(info, totalDamageDealt);
            if (totalDamageDealt > 0f && parent.Spawned && colonyId > 0)
                parent.Map.GetComponent<MapComponent_DesertPitAntColonies>().NotifyMemberDamaged((Pawn)parent, info.Instigator);
        }

        //选中成员时显示所属巢穴。
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (parent.Spawned && colonyId > 0 && parent.Map.GetComponent<MapComponent_DesertPitAntColonies>()
                .TryGetColony((Pawn)parent, out State.AntColonyState state))
                GenDraw.DrawLineBetween(parent.DrawPos, state.Nest.DrawPos, SimpleColor.White, 0.2f);
        }

        //冲锋路线随可见 Pawn 绘制，不要求玩家选中它。
        public override void PostDraw()
        {
            base.PostDraw();
            if (Caste == AntCaste.Breaker && ((Pawn)parent).jobs?.curDriver is Jobs.JobDriver_AntCharge charge)
                charge.DrawIndicator();
        }

        //显示当前冲锋阶段或剩余冷却，便于判断何时能再次使用。
        public override string CompInspectStringExtra()
        {
            if (Caste != AntCaste.Breaker) return null;
            if (((Pawn)parent).jobs?.curDriver is Jobs.JobDriver_AntCharge charge) return charge.GetReport();
            int remaining = NextAbilityTick - Find.TickManager.TicksGame;
            return remaining > 0 ? "冲锋冷却：" + remaining.ToStringTicksToPeriod() : "冲锋：就绪";
        }

        //销毁与死亡事件共享去重登记。
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (previousMap != null && colonyId > 0)
                previousMap.GetComponent<MapComponent_DesertPitAntColonies>().NotifyMemberDestroyed((Pawn)parent, colonyId);
        }
    }
}
