using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NingshaRaceLib.Storyteller.Quests.Parts
{
    //类职责：监视任务指定侵蚀体是否已经被玩家的收容平台正式收容，并发送单次完成信号。
    public sealed class QuestPart_ErosionBodyContained : QuestPartActivable
    {
        //字段职责：任务要求玩家击杀或收容的唯一侵蚀体。
        public Pawn erosionBody;

        //属性职责：让任务界面能够定位指定侵蚀体。
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                if (erosionBody != null)
                {
                    yield return erosionBody;
                }
            }
        }

        //函数职责：逐 tick 检查指定侵蚀体是否位于玩家所属的收容平台上。
        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (erosionBody == null || erosionBody.Dead)
            {
                return;
            }

            CompHoldingPlatformTarget holdingTarget = erosionBody.TryGetComp<CompHoldingPlatformTarget>();
            if (holdingTarget?.CurrentlyHeldOnPlatform == true
                && holdingTarget.HeldPlatform?.Faction == Faction.OfPlayer)
            {
                Complete(erosionBody.Named("SUBJECT"));
            }
        }

        //函数职责：保存并读取任务指定侵蚀体的引用。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref erosionBody, "erosionBody");
        }

        //函数职责：在任务系统替换 Pawn 引用时同步更新监视目标。
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            if (erosionBody == replace)
            {
                erosionBody = with;
            }
        }
    }
}
