using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat
{
    //类职责：提供地刺召唤物的二十五格远程选取、三格宽直线预览和攻击启动逻辑。
    public class Verb_GroundSpikeSummoner : Verb
    {
        //属性职责：返回当前 Verb 使用的地刺召唤物专属参数。
        public VerbProperties_GroundSpikeSummoner Props => (VerbProperties_GroundSpikeSummoner)verbProps;

        //属性职责：让原版把地刺召唤物视为远程武器。
        public override bool IsMeleeAttack => false;

        //函数职责：绘制射程环和从攻击者延伸到当前目标的三格宽直线伤害范围。
        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            if (!target.IsValid || CasterPawn == null || !CasterPawn.Spawned || !target.Cell.InBounds(CasterPawn.Map))
            {
                return;
            }

            List<IntVec3> affectedCells = GroundSpikeCombatUtility.FindAffectedCells(
                CasterPawn.Map,
                CasterPawn.Position,
                target.Cell,
                Props.lineHalfWidth);
            GenDraw.DrawFieldEdges(affectedCells, new Color(0.72f, 0.38f, 0.16f, 0.8f));
        }

        //函数职责：允许选择射程内空地以及敌对 Pawn 和非墙体建筑。
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages)
                || CasterPawn == null
                || !CasterPawn.Spawned
                || target.Cell == CasterPawn.Position)
            {
                return false;
            }

            Thing targetThing = target.Thing;
            return targetThing == null || GroundSpikeCombatUtility.IsDamageTarget(CasterPawn, targetThing);
        }

        //函数职责：固定击退方向并把逐行直线地刺任务登记到游戏组件。
        protected override bool TryCastShot()
        {
            if (!CasterIsPawn
                || !CasterPawn.Spawned
                || !currentTarget.IsValid
                || !currentTarget.Cell.InBounds(CasterPawn.Map)
                || currentTarget.Cell == CasterPawn.Position
                || !CanHitTarget(currentTarget))
            {
                return false;
            }

            IntVec3 origin = CasterPawn.Position;
            IntVec3 targetCell = currentTarget.Cell;
            CasterPawn.rotationTracker.Face(targetCell.ToVector3Shifted());
            Vector3 attackDirection = GroundSpikeCombatUtility.HorizontalDirection(
                origin.ToVector3Shifted(),
                targetCell.ToVector3Shifted());
            GameComponent_GroundSpikeAttacks.Current.Register(this, origin, targetCell, attackDirection);
            lastShotTick = Find.TickManager.TicksGame;
            return true;
        }

        //函数职责：让地刺召唤物攻击按完整攻击周期给予射击技能经验。
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
