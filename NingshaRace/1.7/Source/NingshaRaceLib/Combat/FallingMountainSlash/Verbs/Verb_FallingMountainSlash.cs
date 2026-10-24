using RimWorld;
using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.FallingMountainSlash.Defs;
using NingshaRaceLib.Combat.FallingMountainSlash.Flight;
using NingshaRaceLib.Combat.FallingMountainSlash.Rendering;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.FallingMountainSlash.Verbs
{
    //类职责：验证坠岳斩的地面或 Pawn 目标、创建自定义 PawnFlyer 并启动能力冷却。
    public class Verb_FallingMountainSlash : Verb_CastAbility
    {
        //属性职责：禁止多个 Pawn 共用同一次坠岳斩目标选择。
        public override bool MultiSelect => false;

        //函数职责：在施术者与目标格有效时创建飞行器，并在命中 Pawn 时记录额外重击目标。
        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            Pawn targetPawn = CurrentTarget.Thing as Pawn;
            if (casterPawn == null || !casterPawn.Spawned)
            {
                return false;
            }

            Map map = casterPawn.Map;
            IntVec3 startCell = casterPawn.Position;
            IntVec3 destinationCell = CurrentTarget.Cell;
            if ((targetPawn != null && targetPawn.Map != map)
                || !JumpUtility.ValidJumpTarget(casterPawn, map, destinationCell))
            {
                return false;
            }

            PawnFlyer_FallingMountainSlash flyer = PawnFlyer.MakeFlyer(
                DefOfRefs.NingshaRace_PawnFlyer_FallingMountainSlash,
                casterPawn,
                destinationCell,
                null,
                null,
                triggeringAbility: Ability,
                target: CurrentTarget) as PawnFlyer_FallingMountainSlash;
            if (flyer == null)
            {
                return false;
            }

            FallingMountainSlashDefExtension settings =
                Ability.def.GetModExtension<FallingMountainSlashDefExtension>();
            if (settings == null)
            {
                throw new System.InvalidOperationException("坠岳斩 AbilityDef 缺少玩法参数扩展。");
            }

            flyer.InitializeLandingData(flyer.DestinationPos, targetPawn, settings);
            GenSpawn.Spawn(flyer, startCell, map);
            if (Ability != null)
            {
                int cooldownTicks = Ability.def.cooldownTicksRange.RandomInRange;
                if (cooldownTicks > 0)
                {
                    Ability.StartCooldown(cooldownTicks);
                }
            }

            return true;
        }

        //函数职责：只允许选择施术者以外、位于有效范围内且能够落脚的 Pawn 或地面格。
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            Pawn casterPawn = CasterPawn;
            Pawn targetPawn = target.Thing as Pawn;
            if (casterPawn == null
                || !casterPawn.Spawned
                || !target.IsValid
                || targetPawn == casterPawn
                || target.Cell == casterPawn.Position)
            {
                return false;
            }

            if (casterPawn.Position.DistanceTo(target.Cell) > EffectiveRange)
            {
                if (showMessages)
                {
                    Messages.Message("OutOfRange".Translate(), MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            return JumpUtility.ValidJumpTarget(casterPawn, casterPawn.Map, target.Cell)
                && CanHitTargetFrom(casterPawn.Position, target);
        }

        //函数职责：按十格范围和实际视线判断当前位置能否命中 Pawn 或地面格。
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            Pawn casterPawn = CasterPawn;
            if (casterPawn == null || !target.IsValid || root.DistanceTo(target.Cell) > EffectiveRange)
            {
                return false;
            }

            return GenSight.LineOfSight(root, target.Cell, casterPawn.Map, true);
        }

        //函数职责：绘制坠岳斩有效落脚范围和当前目标高亮。
        public override void DrawHighlight(LocalTargetInfo target)
        {
            Pawn casterPawn = CasterPawn;
            if (casterPawn == null || !casterPawn.Spawned)
            {
                return;
            }

            GenDraw.DrawRadiusRing(
                casterPawn.Position,
                EffectiveRange,
                Color.red,
                cell => JumpUtility.ValidJumpTarget(casterPawn, casterPawn.Map, cell)
                    && GenSight.LineOfSight(casterPawn.Position, cell, casterPawn.Map, true));
            if (target.IsValid && JumpUtility.ValidJumpTarget(casterPawn, casterPawn.Map, target.Cell))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }
        }

        //函数职责：在目标不是有效落脚格或不可命中时显示禁止施放光标。
        public override void OnGUI(LocalTargetInfo target)
        {
            if (ValidateTarget(target, false))
            {
                base.OnGUI(target);
                return;
            }

            GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
        }
    }
}
