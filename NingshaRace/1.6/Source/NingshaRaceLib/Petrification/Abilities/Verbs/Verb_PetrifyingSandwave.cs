using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.Core.Effects;
using NingshaRaceLib.Petrification.Abilities.Components;
using NingshaRaceLib.Petrification.Health;
using NingshaRaceLib.Petrification.Patches;
using NingshaRaceLib.Petrification.Rendering;
using NingshaRaceLib.Petrification.Utility;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.Petrification.Abilities.Verbs
{
    //类职责：负责石化砂潮的目标校验、施法朝向和双层扇形范围预览。
    public class Verb_PetrifyingSandwave : Verb_CastAbility
    {
        //属性职责：禁止多个 Pawn 共用同一次石化砂潮目标选择。
        public override bool MultiSelect => false;

        //属性职责：取得当前能力实例上的石化砂潮效果组件。
        private CompAbilityEffect_PetrifyingSandwave EffectComp =>
            Ability?.CompOfType<CompAbilityEffect_PetrifyingSandwave>();

        //函数职责：验证任意阵营的血肉 Pawn 或有效地面格，并拒绝同格、墙体与无视线目标。
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            Pawn casterPawn = CasterPawn;
            if (casterPawn == null
                || !casterPawn.Spawned
                || !target.IsValid
                || !target.Cell.IsValid
                || !target.Cell.InBounds(casterPawn.Map)
                || target.Cell == casterPawn.Position)
            {
                return false;
            }

            if (!base.ValidateTarget(target, showMessages))
            {
                return false;
            }

            if (target.HasThing)
            {
                bool excludeCasterFaction = EffectComp?.Props.excludeCasterFaction == true;
                if (target.Thing is Pawn pawn
                    && PetrifyingSandwaveUtility.IsValidTarget(casterPawn, pawn, excludeCasterFaction))
                {
                    return true;
                }

                if (showMessages)
                {
                    Messages.Message("石化砂潮只能瞄准其他血肉生物，也可以直接选择地面", casterPawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            if (target.Cell.Filled(casterPawn.Map))
            {
                if (showMessages)
                {
                    Messages.Message("石化砂潮不能朝墙体占据的地格释放", casterPawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            return true;
        }

        //函数职责：绘制受墙体遮挡的十格扇形，并用不同颜色区分必定石化与累计区域。
        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            Pawn casterPawn = CasterPawn;
            CompAbilityEffect_PetrifyingSandwave effectComp = EffectComp;
            if (casterPawn == null
                || !casterPawn.Spawned
                || effectComp == null
                || !target.IsValid
                || !target.Cell.InBounds(casterPawn.Map)
                || target.Cell == casterPawn.Position)
            {
                return;
            }

            List<IntVec3> innerCells = new List<IntVec3>();
            List<IntVec3> outerCells = new List<IntVec3>();
            PetrifyingSandwaveUtility.FindPreviewCells(
                casterPawn,
                target.Cell,
                EffectiveRange,
                effectComp.Props,
                innerCells,
                outerCells);
            GenDraw.DrawFieldEdges(outerCells, new Color(0.86f, 0.66f, 0.31f, 0.78f));
            GenDraw.DrawFieldEdges(innerCells, new Color(0.66f, 0.68f, 0.64f, 0.9f));
        }

        //函数职责：释放前再次锁定面向方向，再交给原版能力激活和冷却流程。
        protected override bool TryCastShot()
        {
            if (!ValidateTarget(CurrentTarget, false))
            {
                return false;
            }

            CasterPawn.rotationTracker?.FaceTarget(CurrentTarget);
            return base.TryCastShot();
        }
    }
}
