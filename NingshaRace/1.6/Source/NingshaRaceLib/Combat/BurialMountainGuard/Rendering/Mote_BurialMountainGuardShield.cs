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
        private const float VisualAlphaMultiplier = 0.72f;
        private static readonly int ShieldStrengthId = Shader.PropertyToID("_ShieldStrength");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        private Pawn sourcePawn;
        private float chargeRatio;
        private MaterialPropertyBlock propertyBlock;

        //函数职责：绑定护盾跟随的 Pawn。
        public void Initialize(Pawn pawn)
        {
            sourcePawn = pawn;
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

        //函数职责：推进生命周期并同步护盾到 Pawn 当前绘制位置。
        protected override void Tick()
        {
            base.Tick();
            if (Destroyed)
            {
                return;
            }

            if (sourcePawn == null || sourcePawn.Destroyed || sourcePawn.Dead || !sourcePawn.Spawned)
            {
                Destroy();
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
