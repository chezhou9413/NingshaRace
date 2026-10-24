using System.Collections.Generic;
using NingshaRaceLib.DesertPit.Antlion.Components;
using Verse;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.Antlion.AI.Jobs
{
    //类职责：复用原版近战驱动，仅增加伏击阶段、猎物状态和移动边界限制。
    public sealed class JobDriver_AntlionAttack : JobDriver_AttackMelee
    {
        //函数职责：每次近战任务更新前检查边界，不允许目标倒地后继续攻击。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompAntlionAmbush comp = pawn.GetComp<CompAntlionAmbush>();
            AddEndCondition(() => CheckAmbushState(comp));
            foreach (Toil toil in base.MakeNewToils()) yield return toil;
        }

        //函数职责：在任务终止前标记受阻，防止清空路径后思考树立即重发同一越界路线。
        private JobCondition CheckAmbushState(CompAntlionAmbush comp)
        {
            if (comp.CanContinueAttack(job.targetA.Pawn)) return JobCondition.Ongoing;
            comp.NotifyPathFailed();
            return JobCondition.InterruptForced;
        }

        //函数职责：记录路径受阻后使用原版失败清理，不设置破门目标。
        public override void Notify_PatherFailed()
        {
            pawn.GetComp<CompAntlionAmbush>().NotifyPathFailed();
            base.Notify_PatherFailed();
        }
    }
}
