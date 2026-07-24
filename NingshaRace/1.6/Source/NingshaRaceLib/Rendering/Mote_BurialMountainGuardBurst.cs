using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering
{
    //类职责：绘制葬岳格挡蓄满后的沙土爆发环。
    public class Mote_BurialMountainGuardBurst : Mote
    {
        private const float VisualAlphaMultiplier = 0.75f;
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        private MaterialPropertyBlock propertyBlock;

        //函数职责：设置爆发环的中心坐标。
        public void Initialize(Vector3 center)
        {
            exactPosition = center;
            SetAltitude(ref exactPosition);
        }

        //函数职责：使用完整生命周期把爆发 Shader 进度从零驱动到一。
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

            float lifespan = Mathf.Max(def.mote.Lifespan, 0.01f);
            float progress = Mathf.Clamp01(AgeSecs / lifespan);
            propertyBlock.Clear();
            propertyBlock.SetFloat(ProgressId, progress);
            propertyBlock.SetFloat(AlphaId, Alpha * VisualAlphaMultiplier);

            Vector3 scale = ExactScale;
            scale.x *= def.graphicData.drawSize.x;
            scale.z *= def.graphicData.drawSize.y;

            Matrix4x4 matrix = Matrix4x4.TRS(DrawPos, Quaternion.identity, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0, null, 0, propertyBlock);
        }

        //函数职责：把爆发环固定到 Mote Def 指定高度。
        private void SetAltitude(ref Vector3 position)
        {
            position.y = def.altitudeLayer.AltitudeFor();
        }
    }
}
