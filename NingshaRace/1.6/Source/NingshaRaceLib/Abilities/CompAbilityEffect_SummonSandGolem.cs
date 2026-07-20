using NingshaRaceLib.SandGolem;
using RimWorld;
using Verse;

namespace NingshaRaceLib.Abilities
{
    //类职责：声明召唤沙傀能力组件的属性类型。
    public class CompProperties_AbilitySummonSandGolem : CompProperties_AbilityEffect
    {
        //构造函数职责：绑定召唤沙傀能力组件实现。
        public CompProperties_AbilitySummonSandGolem()
        {
            compClass = typeof(CompAbilityEffect_SummonSandGolem);
        }
    }

    //类职责：处理召唤沙傀能力的目标校验、旧沙傀收回和新沙傀生成。
    public class CompAbilityEffect_SummonSandGolem : CompAbilityEffect
    {
        //函数职责：执行沙傀召唤效果。
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || caster.Map == null)
            {
                return;
            }

            GameComponent_SandGolemTracker.Current?.RecallThenSummon(caster, target.Cell);
        }

        //函数职责：校验鼠标目标是否可作为沙傀召唤点。
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            Pawn caster = parent.pawn;
            if (!SandGolemUtility.IsPlayerNingshaPawn(caster))
            {
                if (throwMessages)
                {
                    Messages.Message("只有玩家阵营凝砂族可以召唤沙傀", caster, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }

            if (!target.IsValid || !target.Cell.IsValid)
            {
                if (throwMessages)
                {
                    Messages.Message("目标位置无效", caster, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }

            if (!SandGolemUtility.IsValidSandCell(target.Cell, caster.Map, out string rejectReason))
            {
                if (throwMessages)
                {
                    Messages.Message(rejectReason, new LookTargets(caster, target.ToTargetInfo(caster.Map)), MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }

            return true;
        }

        //函数职责：在鼠标旁显示目标校验失败原因。
        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            if (caster?.Map == null || !target.IsValid || !target.Cell.IsValid)
            {
                return null;
            }

            if (!SandGolemUtility.IsValidSandCell(target.Cell, caster.Map, out string rejectReason))
            {
                return rejectReason;
            }

            return null;
        }
    }
}
