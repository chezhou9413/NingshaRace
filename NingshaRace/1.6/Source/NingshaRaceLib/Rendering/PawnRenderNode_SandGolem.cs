using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering
{
    //类职责：提供沙傀全身截图渲染节点。
    public class PawnRenderNode_SandGolem : PawnRenderNode
    {
        //构造函数职责：创建沙傀渲染节点。
        public PawnRenderNode_SandGolem(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }

        //函数职责：创建使用沙偶 Shader 的动态截图 Graphic。
        public override Graphic GraphicFor(Pawn pawn)
        {
            Shader shader = ShaderFor(pawn);
            if (shader == null)
            {
                return null;
            }

            return GraphicDatabase.Get<Graphic_SandGolem>(BaseContent.WhiteTex, shader, Props.drawSize, Color.white, 3000);
        }

        //函数职责：使用固定未翻转面片绘制截图。
        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            return new GraphicMeshSet(MeshPool.GridPlane(Vector2.one));
        }
    }
}
