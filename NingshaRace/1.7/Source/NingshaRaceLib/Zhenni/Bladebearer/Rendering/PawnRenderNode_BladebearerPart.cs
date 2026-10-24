using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //每个图层独立参与渲染树，身体朝向使用对应原图，武器翻转由握持父节点处理。
    public sealed class PawnRenderNode_BladebearerPart : PawnRenderNode
    {
        internal BladebearerPart Part;
        internal CompBladebearerAppearance Owner;
        internal bool DepthOnly;

        //由刀兵组件按当前配置生成节点。
        public PawnRenderNode_BladebearerPart(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { }

        //原画已有左右视图，不自动镜像身体；武器与特效共用同一张网格。
        public override GraphicMeshSet MeshSetFor(Pawn pawn) => new GraphicMeshSet(BladebearerResources.Mesh);
        //材质由 Worker 直接提交，避免进入人形图集缓存。
        public override Graphic GraphicFor(Pawn pawn) => null;
    }
}
