using Verse;

namespace NingshaRaceLib.Erosion.Health.Components
{
    //类职责：在侵蚀体永久 Hediff 上保存任务指定的优先追杀 Pawn。
    public sealed class HediffComp_ErosionPursuitTarget : HediffComp
    {
        private Pawn pursuitTarget;

        //属性职责：取得当前优先追杀目标。
        public Pawn PursuitTarget => pursuitTarget;

        //函数职责：为任务生成的侵蚀体设置唯一优先追杀目标。
        public void SetPursuitTarget(Pawn target)
        {
            pursuitTarget = target;
        }

        //函数职责：在目标彻底失效后清除追杀引用并恢复普通索敌。
        public void ClearPursuitTarget()
        {
            pursuitTarget = null;
        }

        //函数职责：保存并读取跨存档保持的追杀目标引用。
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref pursuitTarget, "pursuitTarget");
        }
    }
}
