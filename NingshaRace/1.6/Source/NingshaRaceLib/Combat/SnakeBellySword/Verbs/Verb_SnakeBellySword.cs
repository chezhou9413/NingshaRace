using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.SnakeBellySword.Rendering;
using NingshaRaceLib.Combat.SnakeBellySword.Tracking;
using NingshaRaceLib.Combat.SnakeBellySword.Utility;
using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Combat.Utility;

namespace NingshaRaceLib.Combat.SnakeBellySword.Verbs
{
    //提供蛇腹剑的远距离扇形挥击。
    public class Verb_SnakeBellySword : Verb
    {
        //属性职责：返回当前 Verb 使用的蛇腹剑专属参数。
        public VerbProperties_SnakeBellySword Props => (VerbProperties_SnakeBellySword)verbProps;

        //属性职责：让原版把蛇腹剑视为可在远距离使用的武器。
        public override bool IsMeleeAttack => false;

        //绘制实际射程环和九十度扇形预览。
        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            if (!target.IsValid || CasterPawn == null || !CasterPawn.Spawned)
            {
                return;
            }

            List<IntVec3> coneCells = SnakeBellySwordCombatUtility.FindConeCells(
                CasterPawn,
                target,
                Props.coneAngle,
                EffectiveRange);
            GenDraw.DrawFieldEdges(coneCells, new Color(0.95f, 0.2f, 0.12f, 0.75f));
        }

        //函数职责：允许直接选取除攻击者自身以外的 Pawn 和建筑。
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages))
            {
                return false;
            }

            return NingshaDamageTargetUtility.IsValid(CasterPawn, target.Thing);
        }

        //让右键攻击、自动索敌与手动选取使用相同的目标限制。
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            return NingshaDamageTargetUtility.IsValid(CasterPawn, target.Thing)
                && base.CanHitTargetFrom(root, target);
        }

        //函数职责：完成攻击者面向并启动与动画帧同步的三段攻击。
        protected override bool TryCastShot()
        {
            if (!CasterIsPawn || !CasterPawn.Spawned || !currentTarget.HasThing || currentTarget.Thing.Map != caster.Map)
            {
                return false;
            }

            Thing primaryTarget = currentTarget.Thing;
            if (!NingshaDamageTargetUtility.IsValid(CasterPawn, primaryTarget)
                || !CanHitTarget(currentTarget))
            {
                return false;
            }

            CasterPawn.rotationTracker.Face(primaryTarget.DrawPos);
            Vector3 attackDirection = SnakeBellySwordCombatUtility.HorizontalDirection(CasterPawn.DrawPos, primaryTarget.DrawPos);
            SnakeBellySwordCombatUtility.BeginAttack(this, attackDirection);
            GameComponent_SnakeBellySwordAttacks.Current.Register(this, attackDirection);

            lastShotTick = Find.TickManager.TicksGame;
            //一次完整挥击只计一次近战经验，不随三段伤害重复发放。
            CasterPawn.skills?.Learn(SkillDefOf.Melee, 200f * verbProps.AdjustedFullCycleTime(this, CasterPawn));
            return true;
        }
    }
}
