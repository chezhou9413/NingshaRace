using Verse;

namespace NingshaRaceLib.SandGolem.Automation
{
    //类职责：保存一名召唤者的自动召唤开关和尚未完成的补召请求。
    public sealed class SandGolemAutoSummonState : IExposable
    {
        public Pawn caster;
        public bool pending;
        public IntVec3 homeSand = IntVec3.Invalid;

        //函数职责：序列化开关所属角色和请求，落点在运行时重新检查。
        public void ExposeData()
        {
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref pending, "pending");
        }
    }
}
