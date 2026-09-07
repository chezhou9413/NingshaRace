using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.AntColony.Effects;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.AntColony.Combat
{
    //类职责：沿用原版弹丸碰撞与伤害流程，绘制酸液尾迹，并让未被盾拦截的命中附带短暂减速。
    public sealed class Projectile_AntAcid : Bullet
    {
        //函数职责：在原版完成移动及碰撞后补出经过路径的尾迹，终止弹丸及离屏跳步不补画。
        protected override void TickInterval(int delta)
        {
            Map map = Map;
            Vector3 previous = ExactPosition;
            base.TickInterval(delta);
            if (Spawned && !landed && delta == 1)
                AntAcidTrailUtility.EmitSegment(map, previous, ExactPosition, thingIDNumber);
        }

        //函数职责：结算真实命中后刷新唯一的酸液黏附状态，不叠加多份移动惩罚。
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);
            if (blockedByShield || !(hitThing is Pawn pawn) || pawn.Dead || pawn.Destroyed) return;
            Hediff slow = pawn.health.hediffSet.GetFirstHediffOfDef(DefOfRefs.NingshaRace_AntAcidSlow);
            if (slow == null) pawn.health.AddHediff(DefOfRefs.NingshaRace_AntAcidSlow);
            else slow.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 180;
        }
    }
}
