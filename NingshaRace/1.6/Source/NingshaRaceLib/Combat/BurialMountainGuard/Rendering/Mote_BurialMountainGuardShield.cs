using UnityEngine;
using Verse;

using NingshaRaceLib.Combat.BurialMountainGuard.Components;
using NingshaRaceLib.Combat.BurialMountainGuard.Utility;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.Combat.BurialMountainGuard.Rendering
{
    //类职责：绘制跟随葬岳持有者的沙暴护盾，并把蓄力映射为一到二的护盾强度。
    public class Mote_BurialMountainGuardShield : Mote
    {
        //常量职责：限制护盾透明度，避免自发光效果遮挡 Pawn。
        private const float VisualAlphaMultiplier = 0.72f;

        //字段职责：缓存护盾强度 Shader 属性编号。
        private static readonly int ShieldStrengthId = Shader.PropertyToID("_ShieldStrength");

        //字段职责：缓存透明度 Shader 属性编号。
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        //字段职责：保存护盾需要跟随和校验的持剑 Pawn。
        private Pawn sourcePawn;

        //字段职责：缓存当前武器格挡组件，避免每 Tick 扫描装备组件列表。
        private Comp_BurialMountainGuardMode guardComp;

        //字段职责：保存零到一的护盾蓄力显示比例。
        private float chargeRatio;

        //字段职责：复用单个材质属性块，避免每次绘制产生托管分配。
        private MaterialPropertyBlock propertyBlock;

        //函数职责：绑定护盾跟随的 Pawn 和对应武器格挡组件。
        public void Initialize(Pawn pawn, Comp_BurialMountainGuardMode comp)
        {
            sourcePawn = pawn;
            guardComp = comp;
            exactPosition = pawn.DrawPos;
            SetAltitude(ref exactPosition);
            Maintain();
        }

        //函数职责：保存护盾跟随对象和当前视觉参数。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref sourcePawn, "sourcePawn");
            Scribe_Values.Look(ref chargeRatio, "chargeRatio", 0f);
        }

        //函数职责：接收格挡蓄力比例并限制在零到一。
        public void UpdateVisuals(float newChargeRatio)
        {
            chargeRatio = Mathf.Clamp01(newChargeRatio);
        }

        //函数职责：只在持有者仍使用同一把葬岳格挡时维持生命周期并同步显示状态。
        protected override void Tick()
        {
            if (sourcePawn == null || sourcePawn.Destroyed || sourcePawn.Dead || !sourcePawn.Spawned)
            {
                Destroy();
                return;
            }

            if (guardComp == null || guardComp.parent != sourcePawn.equipment?.Primary)
            {
                if (!BurialMountainGuardUtility.TryGetGuardComp(sourcePawn, out guardComp))
                {
                    Destroy();
                    return;
                }
            }

            if (!guardComp.GuardMode)
            {
                Destroy();
                return;
            }

            Maintain();
            UpdateVisuals(guardComp.ChargeRatio);
            base.Tick();
            if (Destroyed)
            {
                return;
            }

            exactPosition = sourcePawn.DrawPos;
            SetAltitude(ref exactPosition);
            UpdateMapCell();
        }

        //函数职责：用独立属性块把零到一蓄力映射为一到二的护盾强度。
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (paused || Find.UIRoot.HideMotes)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();
            propertyBlock.SetFloat(ShieldStrengthId, 1f + chargeRatio);
            propertyBlock.SetFloat(AlphaId, Alpha * VisualAlphaMultiplier);

            Vector3 scale = ExactScale;
            scale.x *= def.graphicData.drawSize.x;
            scale.z *= def.graphicData.drawSize.y;

            Matrix4x4 matrix = Matrix4x4.TRS(DrawPos, Quaternion.identity, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0, null, 0, propertyBlock);
        }

        //函数职责：把护盾固定到 Mote Def 指定高度。
        private void SetAltitude(ref Vector3 position)
        {
            position.y = def.altitudeLayer.AltitudeFor();
        }

        //函数职责：同步 Thing 所在格，保证跟随护盾能在地图当前视野绘制。
        private void UpdateMapCell()
        {
            if (Map == null)
            {
                return;
            }

            IntVec3 cell = exactPosition.ToIntVec3();
            if (cell.InBounds(Map))
            {
                Position = cell;
            }
        }
    }
}
