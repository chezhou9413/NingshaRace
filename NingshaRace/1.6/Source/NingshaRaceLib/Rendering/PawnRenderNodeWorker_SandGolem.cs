using NingshaRaceLib.SandGolem;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Rendering
{
    //类职责：控制沙傀截图节点的显示条件和 Shader 动画参数。
    public class PawnRenderNodeWorker_SandGolem : PawnRenderNodeWorker
    {
        //字段职责：缓存沙偶 Shader 进度属性编号。
        private static readonly int SandProgressPropertyId = Shader.PropertyToID("_SandProgress");

        //函数职责：只允许沙傀 Pawn 绘制本节点。
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms) || !SandGolemUtility.IsSandGolem(parms.pawn))
            {
                return false;
            }

            GameComponent_SandGolemTracker tracker = GameComponent_SandGolemTracker.Current;
            if (tracker == null || !tracker.TryGetState(parms.pawn, out SandGolemRenderState state))
            {
                return false;
            }

            return state.HasMaterialFor(state.DrawFacingFor(parms.facing));
        }

        //函数职责：写入沙傀截图纹理对应的动画进度。
        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            MaterialPropertyBlock block = base.GetMaterialPropertyBlock(node, material, parms);
            if (block == null)
            {
                block = node.MatPropBlock;
            }

            GameComponent_SandGolemTracker tracker = GameComponent_SandGolemTracker.Current;
            if (tracker != null && tracker.TryGetState(parms.pawn, out SandGolemRenderState state))
            {
                block.SetFloat(SandProgressPropertyId, state.SandProgressAt(Find.TickManager.TicksGame));
            }

            return block;
        }
    }
}
