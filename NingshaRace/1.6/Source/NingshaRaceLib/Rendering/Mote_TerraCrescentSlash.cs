using RimWorld;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering
{
    //类职责：让土元素月牙刀光跟随飞行 Pawn，并独立驱动展开、保持和收束参数。
    public class Mote_TerraCrescentSlash : Mote
    {
        private const float ArcLimit = 3.14f;
        private const float RevealDuration = 0.3f;
        private const float HoldDuration = 0.3f;
        private const float DismissDuration = 0.3f;
        private static readonly int ArcStartId = Shader.PropertyToID("_ArcStart");
        private static readonly int ArcEndId = Shader.PropertyToID("_ArcEnd");

        private PawnFlyer sourceFlyer;
        private Vector3 targetPosition;
        private bool positionLocked;
        private MaterialPropertyBlock propertyBlock;

        //函数职责：记录刀光跟随来源、最终停留坐标和对齐跳劈方向的旋转角。
        public void Initialize(PawnFlyer flyer, Vector3 destination, float rotation)
        {
            sourceFlyer = flyer;
            targetPosition = destination;
            exactPosition = flyer.DrawPos;
            exactRotation = rotation;
            SetAltitude(ref exactPosition);
            SetAltitude(ref targetPosition);
        }

        //函数职责：保存跟随引用、停留坐标和锁定状态。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref sourceFlyer, "sourceFlyer");
            Scribe_Values.Look(ref targetPosition, "targetPosition");
            Scribe_Values.Look(ref positionLocked, "positionLocked", false);
            Scribe_Values.Look(ref exactPosition, "exactPosition");
            Scribe_Values.Look(ref exactRotation, "exactRotation", 0f);
        }

        //函数职责：推进 Mote 生命周期，并在 Flyer 落地前同步其可视位置。
        protected override void Tick()
        {
            base.Tick();
            if (Destroyed || positionLocked)
            {
                return;
            }

            if (sourceFlyer != null && !sourceFlyer.Destroyed && sourceFlyer.Spawned)
            {
                exactPosition = sourceFlyer.DrawPos;
                SetAltitude(ref exactPosition);
                UpdateMapCell();
                return;
            }

            exactPosition = targetPosition;
            SetAltitude(ref exactPosition);
            UpdateMapCell();
            positionLocked = true;
            sourceFlyer = null;
        }

        //函数职责：使用独立属性块绘制当前时间点的月牙展开或收束状态。
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

            float arcStart;
            float arcEnd;
            EvaluateArcTimeline(out arcStart, out arcEnd);
            propertyBlock.Clear();
            propertyBlock.SetFloat(ArcStartId, arcStart);
            propertyBlock.SetFloat(ArcEndId, arcEnd);

            Vector3 scale = ExactScale;
            scale.x *= def.graphicData.drawSize.x;
            scale.z *= def.graphicData.drawSize.y;
            Matrix4x4 matrix = Matrix4x4.TRS(
                DrawPos,
                Quaternion.AngleAxis(exactRotation, Vector3.up),
                scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0, null, 0, propertyBlock);
        }

        //函数职责：按十八 Tick 展开、十八 Tick 保持和十八 Tick 收束计算 Shader 弧线参数。
        private void EvaluateArcTimeline(out float arcStart, out float arcEnd)
        {
            float age = AgeSecs;
            if (age < RevealDuration)
            {
                float progress = Mathf.SmoothStep(0f, 1f, age / RevealDuration);
                arcStart = Mathf.Lerp(ArcLimit, -ArcLimit, progress);
                arcEnd = ArcLimit;
                return;
            }

            if (age < RevealDuration + HoldDuration)
            {
                arcStart = -ArcLimit;
                arcEnd = ArcLimit;
                return;
            }

            float dismissAge = age - RevealDuration - HoldDuration;
            float dismissProgress = Mathf.SmoothStep(0f, 1f, dismissAge / DismissDuration);
            arcStart = -ArcLimit;
            arcEnd = Mathf.Lerp(ArcLimit, -ArcLimit, dismissProgress);
        }

        //函数职责：把刀光放在固定的 MoteOverhead 绘制高度。
        private void SetAltitude(ref Vector3 position)
        {
            position.y = def.altitudeLayer.AltitudeFor();
        }

        //函数职责：同步 Thing 所在格，保证移动刀光在地图动态绘制范围内。
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
