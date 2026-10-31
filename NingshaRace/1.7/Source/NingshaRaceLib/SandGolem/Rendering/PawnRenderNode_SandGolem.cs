using UnityEngine;
using Verse;

using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.SandGolem.Health;
using NingshaRaceLib.SandGolem.Lifecycle;
using NingshaRaceLib.SandGolem.Tracking;
using NingshaRaceLib.SandGolem.Utility;

namespace NingshaRaceLib.SandGolem.Rendering
{
    //提供沙傀全身截图渲染节点。
    public class PawnRenderNode_SandGolem : PawnRenderNode
    {
        //创建沙傀渲染节点。
        public PawnRenderNode_SandGolem(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }

        //节点持有动态截图图形，由渲染树初始化流程缓存。
        public override Graphic GraphicFor(Pawn pawn)
        {
            Shader shader = ShaderFor(pawn);
            if (shader == null)
            {
                return null;
            }

            //纹理请求没有固定路径，不进入会在计算哈希时改写路径的全局图形缓存。
            var graphic = new Graphic_SandGolem();
            graphic.Init(new GraphicRequest(typeof(Graphic_SandGolem), BaseContent.WhiteTex, shader,
                Props.drawSize, Color.white, Color.white, null, 3000, null, null));
            return graphic;
        }

        //使用固定未翻转面片绘制截图。
        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            return new GraphicMeshSet(MeshPool.GridPlane(Vector2.one));
        }
    }
}
