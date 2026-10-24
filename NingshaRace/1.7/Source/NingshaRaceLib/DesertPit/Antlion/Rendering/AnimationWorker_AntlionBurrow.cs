using NingshaRaceLib.DesertPit.Antlion.Components;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Rendering
{
    //类职责：通过原版动画节点缩放身体表现出入沙地，不使用独立材质或全局 Pawn 渲染补丁。
    public sealed class AnimationWorker_AntlionBurrow : AnimationWorker_Keyframes
    {
        //函数职责：仅在存活且能行动的蚁狮切换阶段启用动画，尸体和倒地身体保持原尺寸。
        public override bool Enabled(AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            CompAntlionAmbush comp = parms.pawn.GetComp<CompAntlionAmbush>();
            return comp != null && comp.AbleToAct && comp.Transitioning;
        }

        //函数职责：以平滑曲线展开或收拢身体，使 XML 中的阶段时长直接控制动画进度。
        public override Vector3 ScaleAtTick(int tick, AnimationDef def, PawnRenderNode node,
            AnimationPart part, PawnDrawParms parms)
        {
            float amount = parms.pawn.GetComp<CompAntlionAmbush>().SurfaceFraction;
            float smooth = amount * amount * (3f - 2f * amount);
            return new Vector3(Mathf.Lerp(0.12f, 1f, smooth), 1f, Mathf.Lerp(0.04f, 1f, smooth));
        }
    }
}
