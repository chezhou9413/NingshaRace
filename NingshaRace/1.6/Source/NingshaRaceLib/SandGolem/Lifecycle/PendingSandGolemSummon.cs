using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Rendering;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Lifecycle
{
    //类职责：保存旧沙傀消散完成后需要执行的新沙傀召唤请求。
    public class PendingSandGolemSummon : IExposable
    {
        //字段职责：记录等待召唤新沙傀的施法者。
        public Pawn caster;

        //字段职责：记录新沙傀目标地格。
        public IntVec3 targetCell;

        //字段职责：记录允许生成新沙傀的游戏 Tick。
        public int executeTick;

        //构造函数职责：为 Scribe 反序列化提供空实例。
        public PendingSandGolemSummon()
        {
        }

        //构造函数职责：创建一个延迟召唤请求。
        public PendingSandGolemSummon(Pawn caster, IntVec3 targetCell, int executeTick)
        {
            this.caster = caster;
            this.targetCell = targetCell;
            this.executeTick = executeTick;
        }

        //函数职责：保存和读取延迟召唤请求。
        public void ExposeData()
        {
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref targetCell, "targetCell");
            Scribe_Values.Look(ref executeTick, "executeTick");
        }
    }
}
