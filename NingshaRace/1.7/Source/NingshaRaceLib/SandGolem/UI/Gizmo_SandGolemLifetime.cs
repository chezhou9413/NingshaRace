using RimWorld;
using Verse;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Utility;
using NingshaRaceLib.UI.Gizmos;

namespace NingshaRaceLib.SandGolem.UI
{
    //类职责：将沙傀生命周期映射为可展开的存在时间石板，不改变到期与消散流程。
    public sealed class Gizmo_SandGolemLifetime : Gizmo_NingshaStatus
    {
        public SandGolemRenderState state;

        //构造职责：将寿命石板排列在普通沙傀命令之前。
        public Gizmo_SandGolemLifetime() { Order = -110f; }

        //属性职责：以当前游戏 tick 提供真实剩余时长、生命周期阶段与规则说明。
        protected override string Title => "沙傀存在时间";
        protected override string Value => state.RemainingLifetimeTicksAt(Find.TickManager.TicksGame).ToStringTicksToPeriod(allowSeconds: false, shortForm: true);
        protected override string Detail => state.phase == SandGolemPhase.Gathering ? "汇聚完成后开始计时" : "到期后自动消散";
        protected override float Fraction => state.LifetimeRatioAt(Find.TickManager.TicksGame);
        protected override string Help => "沙傀完成汇聚后可以稳定存在 "
            + SandGolemUtility.LifetimeTicks.ToStringTicksToPeriod(allowSeconds: false)
            + "。\n\n当前剩余 " + Value + "，耗尽后会原地消散。";
    }
}
