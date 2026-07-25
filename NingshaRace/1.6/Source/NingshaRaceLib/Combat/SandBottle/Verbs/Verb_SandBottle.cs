using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.SandBottle.Utility;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Utility;

namespace NingshaRaceLib.Combat.SandBottle.Verbs
{
        //类职责：提供沙瓶目标选取、扇形预览和一次性喷砂攻击入口。
    public class Verb_SandBottle : Verb
    {
        //属性职责：返回当前 Verb 使用的沙瓶专属参数。
        public VerbProperties_SandBottle Props => (VerbProperties_SandBottle)verbProps;

        //属性职责：让原版把沙瓶识别为远程武器。
        public override bool IsMeleeAttack => false;

        //函数职责：绘制六格射程环和受视线阻挡的六十度扇形范围。
        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            if (!target.IsValid || CasterPawn == null || !CasterPawn.Spawned || !target.Cell.InBounds(CasterPawn.Map))
            {
                return;
            }

            List<IntVec3> coneCells = SandBottleCombatUtility.FindConeCells(
                CasterPawn,
                target.Cell,
                Props.coneAngle,
                EffectiveRange);
            GenDraw.DrawFieldEdges(coneCells, new Color(0.92f, 0.68f, 0.27f, 0.78f));
        }

        //函数职责：允许选择同一地图上存活的敌对、中立或友方 Pawn 与建筑。
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (CasterPawn == null || !CasterPawn.Spawned)
            {
                return false;
            }

            Thing targetThing = target.Thing;
            return SandBottleCombatUtility.IsValidDamageTarget(CasterPawn, targetThing);
        }

        //函数职责：固定当前喷砂方向，播放粒子并立即结算扇形内全部有效目标。
        protected override bool TryCastShot()
        {
            if (!CasterIsPawn
                || !CasterPawn.Spawned
                || !currentTarget.HasThing
                || !SandBottleCombatUtility.IsValidDamageTarget(CasterPawn, currentTarget.Thing)
                || !CanHitTarget(currentTarget))
            {
                return false;
            }

            Thing primaryTarget = currentTarget.Thing;
            IntVec3 origin = CasterPawn.Position;
            Vector3 attackDirection = SandBottleCombatUtility.HorizontalDirection(
                origin.ToVector3Shifted(),
                primaryTarget.Position.ToVector3Shifted());

            CasterPawn.rotationTracker.Face(primaryTarget.DrawPos);
            SandBottleCombatUtility.SpawnEffect(this, attackDirection);
            SandBottleCombatUtility.ApplyAttack(this, origin, attackDirection);
            lastShotTick = Find.TickManager.TicksGame;
            return true;
        }

        //函数职责：让沙瓶攻击按完整攻击周期给予射击技能经验。
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (CasterIsPawn && CasterPawn.skills != null)
            {
                CasterPawn.skills.Learn(SkillDefOf.Shooting, 200f * verbProps.AdjustedFullCycleTime(this, CasterPawn));
            }
        }
    }
}
