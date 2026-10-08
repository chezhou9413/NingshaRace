using UnityEngine;
using Verse;
using NingshaRaceLib.Core.Defs;

namespace NingshaRaceLib.SandGolem.Rendering
{
    //直接绘制聚散阶段的截图，没有可选取或受击的 Pawn。
    internal static class SandGolemAnimationRenderer
    {
        private static readonly int ProgressId = Shader.PropertyToID("_SandProgress");
        private static MaterialPropertyBlock properties;

        //沿用实体渲染树的尺寸、材质和正面截图。
        public static void Draw(SandGolemRenderState state, int tick)
        {
            if (state.phase == SandGolemPhase.Stable || state.animationMap != Find.CurrentMap
                || state.animationMap == null || state.animationPosition.ToIntVec3().Fogged(state.animationMap)) return;
            Material material = state.MaterialFor(Rot4.South);
            if (material == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetFloat(ProgressId, state.SandProgressAt(tick));
            PawnRenderNodeProperties node = DefOfRefs.NingshaRace_SandGolem.race.renderTree.root.children[0];
            Vector3 position = state.animationPosition;
            position.y = AltitudeLayer.Pawn.AltitudeFor() + PawnRenderUtility.AltitudeForLayer(node.baseLayer);
            Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity,
                new Vector3(node.drawSize.x, 1f, node.drawSize.y));
            Graphics.DrawMesh(MeshPool.GridPlane(Vector2.one), matrix, material, 0, null, 0, properties);
        }
    }
}
