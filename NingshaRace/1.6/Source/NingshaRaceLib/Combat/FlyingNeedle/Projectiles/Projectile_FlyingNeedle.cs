using RimWorld;
using Verse;

using NingshaRaceLib.Petrification.Utility;

namespace NingshaRaceLib.Combat.FlyingNeedle.Projectiles
{
    //类职责：执行飞针原版远程伤害，并让每次有效命中累积同一石化状态。
    public class Projectile_FlyingNeedle : Bullet
    {
        //属性职责：返回当前投射物 Def 中声明的飞针专属参数。
        private ProjectileProperties_FlyingNeedle Props => (ProjectileProperties_FlyingNeedle)def.projectile;

        //函数职责：完成原版命中伤害后，为仍存活的血肉 Pawn 增加配置的石化进度。
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);
            if (blockedByShield
                || !(hitThing is Pawn hitPawn)
                || hitPawn.Dead
                || !hitPawn.RaceProps.IsFlesh)
            {
                return;
            }

            PetrificationUtility.AddSeverity(hitPawn, Props.petrificationSeverity);
        }
    }
}
