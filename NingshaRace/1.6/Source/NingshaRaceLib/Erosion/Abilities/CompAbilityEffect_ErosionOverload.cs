using System;
using RimWorld;
using Verse;

using NingshaRaceLib.Erosion.Components;
using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Abilities
{
    //类职责：校验侵蚀过载的使用条件、确认满值风险并重置两项固有能力冷却。
    public sealed class CompAbilityEffect_ErosionOverload : CompAbilityEffect
    {
        //属性职责：返回侵蚀过载专属能力参数。
        public new CompProperties_AbilityErosionOverload Props =>
            (CompProperties_AbilityErosionOverload)props;

        //函数职责：在 Pawn 不再适用侵蚀系统或没有固有能力冷却时禁用按钮。
        public override bool GizmoDisabled(out string reason)
        {
            Pawn pawn = parent.pawn;
            CompNingshaErosion erosion = pawn?.TryGetComp<CompNingshaErosion>();
            if (!ErosionPawnUtility.IsNormalPlayerNingsha(pawn) || erosion == null)
            {
                reason = "只有尚未实体化的玩家凝砂族可以进行侵蚀过载";
                return true;
            }
            if (erosion.IsTransforming)
            {
                reason = "侵蚀体转化已经开始";
                return true;
            }
            if (!ErosionPawnUtility.HasInnateAbilityOnCooldown(pawn))
            {
                reason = "凝砂之眼和召唤沙傀均未处于冷却";
                return true;
            }

            reason = null;
            return false;
        }

        //函数职责：仅在本次过载会达到侵蚀上限时弹出不可逆实体化确认。
        public override Window ConfirmationDialog(LocalTargetInfo target, Action confirmAction)
        {
            Pawn pawn = parent.pawn;
            CompNingshaErosion erosion = pawn?.TryGetComp<CompNingshaErosion>();
            if (erosion == null || !erosion.WouldReachLimit(Props.erosionGain))
            {
                return null;
            }

            string text = pawn.LabelShortCap
                + "的侵蚀值将达到上限，并在起身动画结束后永久转化为敌对侵蚀体。该过程不可逆，是否继续？";
            return Dialog_MessageBox.CreateConfirmation(text, confirmAction, destructive: true);
        }

        //函数职责：增加侵蚀值并只清除凝砂之眼和召唤沙傀的冷却。
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            CompNingshaErosion erosion = pawn?.TryGetComp<CompNingshaErosion>();
            if (!ErosionPawnUtility.IsNormalPlayerNingsha(pawn)
                || erosion == null
                || erosion.IsTransforming
                || !ErosionPawnUtility.HasInnateAbilityOnCooldown(pawn))
            {
                return;
            }

            erosion.AddErosion(Props.erosionGain);
            ErosionPawnUtility.ResetInnateAbilityCooldowns(pawn);
        }
    }
}
