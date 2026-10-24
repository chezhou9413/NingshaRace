using RimWorld;
using NingshaRaceLib.Combat.MeleeAnimation;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //原版持械节点负责深度、姿态与瞄准，刀上特效只接收最终装备矩阵。
    public sealed class PawnRenderNodeWorker_BladebearerWeapon : PawnRenderNodeWorker_Carried
    {
        //倒地、死亡、收容和失去装备时不显示武器。
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
            => ((PawnRenderNode_BladebearerWeapon)node).Ready && parms.posture == PawnPosture.Standing
                && base.CanDrawNow(node, parms);

        //地图直接执行原版持械流程；美术预览使用原版闲置持刀姿态。
        public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
        {
            if (parms.pawn.TryGetComp<NingshaRaceLib.Zhenni.Bowbearer.CompBowbearerShooting>() is { } bow)
            {
                //弓使用原版远程持械流程，不进入近战动画状态。
                var previous = NingshaRaceLib.Zhenni.Bowbearer.BowbearerWeaponDraws.Active;
                NingshaRaceLib.Zhenni.Bowbearer.BowbearerWeaponDraws.Active =
                    new NingshaRaceLib.Zhenni.Bowbearer.BowbearerWeaponDraws.Context((PawnRenderNode_BladebearerWeapon)node, parms, bow);
                try
                {
                    if (parms.DrawNow && bow.PreviewActive)
                    {
                        float aim = NingshaRaceLib.Zhenni.Bowbearer.BowbearerWeaponDraws.PreviewAim(parms.facing);
                        PawnRenderUtility.DrawEquipmentAiming(parms.pawn.equipment.Primary, parms.matrix.Position(), aim);
                    }
                    else if (parms.DrawNow)
                        PawnRenderUtility.DrawCarriedWeapon(parms.pawn.equipment.Primary, parms.matrix.Position(), parms.facing,
                            parms.pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor);
                    else base.PostDraw(node, parms, mesh, matrix);
                }
                finally { NingshaRaceLib.Zhenni.Bowbearer.BowbearerWeaponDraws.Active = previous; }
                return;
            }
            PawnRenderNode_BladebearerWeapon previousNode = BladebearerWeaponDraws.ActiveNode;
            PawnDrawParms previousParms = BladebearerWeaponDraws.ActiveParms;
            BladebearerWeaponDraws.ActiveNode = (PawnRenderNode_BladebearerWeapon)node;
            BladebearerWeaponDraws.ActiveParms = parms;
            MeleeWeaponDraws.Context previousContext = MeleeWeaponDraws.Current;
            MeleeWeaponDraws.Current = new MeleeWeaponDraws.Context
            {
                Owner = parms.pawn.TryGetComp<CompMeleeAnimation>(), Parms = parms,
                Blade = BladebearerWeaponDraws.BladeTransform(), DrawLayers = BladebearerWeaponDraws.DrawLayers
            };
            try
            {
                MeleeAnimationPlayback play = MeleeWeaponDraws.Current.Owner.Current;
                if (play.Active && play.Facing == parms.facing)
                {
                    //保留原版瞄准持械距离，仅锁定本次攻击的目标角度。
                    Thing weapon = parms.pawn.equipment.Primary;
                    Vector3 position = parms.matrix.Position() + OffsetFor(node, parms, out _);
                    position += new Vector3(0, 0, 0.4f + weapon.def.equippedDistanceOffset).RotatedBy(play.AimAngle)
                        * parms.pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
                    PawnRenderUtility.DrawEquipmentAiming(weapon, position, play.AimAngle);
                }
                else if (parms.DrawNow)
                {
                    Vector3 position = parms.matrix.Position() + OffsetFor(node, parms, out _);
                    PawnRenderUtility.DrawCarriedWeapon(parms.pawn.equipment.Primary, position, parms.facing,
                        parms.pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor);
                }
                else base.PostDraw(node, parms, mesh, matrix);
            }
            finally
            {
                BladebearerWeaponDraws.ActiveNode = previousNode;
                BladebearerWeaponDraws.ActiveParms = previousParms;
                MeleeWeaponDraws.Current = previousContext;
            }
        }
    }
}
