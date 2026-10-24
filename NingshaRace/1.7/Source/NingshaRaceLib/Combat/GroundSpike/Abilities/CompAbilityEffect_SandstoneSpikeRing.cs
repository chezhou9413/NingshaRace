using System;
using RimWorld;
using Verse;

using NingshaRaceLib.Combat.GroundSpike.Tracking;

namespace NingshaRaceLib.Combat.GroundSpike.Abilities
{
    //类职责：在砂岩棘环施放完成时登记以施术者当前位置为圆心的扩散攻击。
    public sealed class CompAbilityEffect_SandstoneSpikeRing : CompAbilityEffect
    {
        //属性职责：返回砂岩棘环专属能力参数。
        public new CompProperties_AbilitySandstoneSpikeRing Props =>
            (CompProperties_AbilitySandstoneSpikeRing)props;

        //函数职责：固定释放圆心并把环形地刺任务登记到当前游戏组件。
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned)
            {
                throw new InvalidOperationException("砂岩棘环施术者未生成，无法登记攻击序列。");
            }

            GameComponent_GroundSpikeAttacks.Current.RegisterRing(caster, caster.Position, Props);
        }
    }
}
