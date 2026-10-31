using NingshaRaceLib.DesertPit.AntColony.Jobs;
using Verse.AI;

namespace NingshaRaceLib.DesertPit.AntColony.Combat
{
    //技能开始后保持执行，失去行动能力时交还原版清理。
    internal static class AntAbilityCommitment
    {
        //只锁定两种蚁群位移任务，不改变其他角色或其他工作。
        public static bool IsLocked(JobDriver driver) =>
            (driver is JobDriver_AntCharge || driver is JobDriver_AntJump)
            && !driver.ended && driver.pawn.Spawned && !driver.pawn.Dead && !driver.pawn.Downed;
    }
}
