using NingshaRaceLib.Core.Defs;
using NingshaRaceLib.DesertPit.Antlion.Components;
using Verse;

namespace NingshaRaceLib.DesertPit.Antlion.Rendering
{
    //类职责：在游戏更新阶段绑定原版动画管线，并按存档中的阶段时间恢复出入沙地表现。
    internal static class AntlionAnimationUtility
    {
        //函数职责：仅更换本模块动画，不加载线程静态纹理，也不覆盖其他正常阶段动画。
        public static void Synchronize(CompAntlionAmbush comp)
        {
            PawnRenderer renderer = comp.Pawn.Drawer.renderer;
            if (comp.AbleToAct && comp.Transitioning)
            {
                if (renderer.renderTree.currentAnimation != DefOfRefs.NingshaRace_AntlionBurrowing)
                    renderer.SetAnimation(DefOfRefs.NingshaRace_AntlionBurrowing);
                renderer.renderTree.animationStartTick = comp.PhaseStartTick;
            }
            else if (renderer.renderTree.currentAnimation == DefOfRefs.NingshaRace_AntlionBurrowing)
                renderer.SetAnimation(null);
        }
    }
}
