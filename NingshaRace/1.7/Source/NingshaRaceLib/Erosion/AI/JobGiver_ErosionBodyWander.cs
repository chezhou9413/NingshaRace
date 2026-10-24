using RimWorld;
using Verse;
using Verse.AI;

using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Erosion.AI
{
    //类职责：让没有战斗目标的侵蚀体以低频率缓慢游荡，并靠近同阵营侵蚀体群体。
    public sealed class JobGiver_ErosionBodyWander : JobGiver_Wander
    {
        //字段职责：定义每次思考后实际移动而不是原地等待的概率。
        private const float WanderChance = 0.2f;

        //构造函数职责：配置侵蚀体的漫步速度和两次游荡之间的等待区间。
        public JobGiver_ErosionBodyWander()
        {
            locomotionUrgency = LocomotionUrgency.Amble;
            ticksBetweenWandersRange = new IntRange(480, 900);
        }

        //函数职责：按低概率选择移动，其余时间保持蹒跚怪式停顿。
        protected override Job TryGiveJob(Pawn pawn)
        {
            pawn.mindState.nextMoveOrderIsWait = !Rand.Chance(WanderChance);
            return base.TryGiveJob(pawn);
        }

        //函数职责：以附近同阵营侵蚀体为群体游荡中心。
        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            return WanderUtility.GetHerdWanderRoot(
                pawn,
                thing => thing is Pawn other
                    && other != pawn
                    && other.IsMutant
                    && other.mutant.Def == DefOfRefs.NingshaRace_ErosionBodyMutant
                    && other.Faction == pawn.Faction);
        }
    }
}
