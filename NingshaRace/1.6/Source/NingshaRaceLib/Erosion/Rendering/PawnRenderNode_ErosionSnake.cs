using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Rendering
{
    //倒地翻头时同步蛇头网格方向，保持西向镜像与连接点一致。
    public sealed class PawnRenderNode_ErosionSnake : PawnRenderNode
    {
        //复用原版图形、材质与网格缓存。
        public PawnRenderNode_ErosionSnake(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { }

        //与蛇头材质使用同一个有效朝向。
        public override Mesh GetMesh(PawnDrawParms parms)
        {
            if (parms.flipHead) parms.facing = parms.facing.Opposite;
            return base.GetMesh(parms);
        }
    }
}
