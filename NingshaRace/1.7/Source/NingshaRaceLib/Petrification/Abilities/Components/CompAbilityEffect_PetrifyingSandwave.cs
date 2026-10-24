using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Health;
using NingshaRaceLib.Petrification.Patches;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.Petrification.Utility;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.Petrification.Abilities.Components
{
    //类职责：在石化砂潮能力成功激活时固定方向并触发范围结算。
    public class CompAbilityEffect_PetrifyingSandwave : CompAbilityEffect
    {
        //属性职责：返回石化砂潮专属能力参数。
        public new CompProperties_AbilityPetrifyingSandwave Props =>
            (CompProperties_AbilityPetrifyingSandwave)props;

        //属性职责：让玩家和战斗决策都只能在施法者存活、落地且未倒地时选择砂潮。
        public override bool CanCast => base.CanCast && parent.pawn.Spawned
            && !parent.pawn.Dead && !parent.pawn.Downed;

        //函数职责：在结算前确认施法者仍可行动，再按释放瞬间的位置与方向结算砂潮。
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Dead || caster.Downed)
            {
                return;
            }

            base.Apply(target, dest);
            caster.rotationTracker?.FaceTarget(target);
            Vector3 direction = PetrifyingSandwaveUtility.HorizontalDirection(
                caster.Position.ToVector3Shifted(),
                target.Cell.ToVector3Shifted());
            PetrifyingSandwaveUtility.ApplyWave(
                caster,
                target.Cell,
                direction,
                parent.verb.EffectiveRange,
                Props);
        }
    }
}
