using Verse;

namespace NingshaRaceLib.Combat.FlyingNeedle.Projectiles
{
    //类职责：保存飞针投射物的原版弹道参数和每次命中的石化进度增量。
    public class ProjectileProperties_FlyingNeedle : ProjectileProperties
    {
        //字段职责：定义飞针命中存活血肉 Pawn 时增加的石化严重度。
        public float petrificationSeverity = 0.25f;
    }
}
