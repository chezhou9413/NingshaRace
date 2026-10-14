using RimWorld.Planet;
using UnityEngine;
using Verse;
using NingshaRaceLib.Erosion.Utility;

namespace NingshaRaceLib.Erosion.Effects;

//头部绘制入口只提交最终姿态，循环粒子交由地图组件管理。
public static class ErosionHeadEffectManager
{
    //继承倒地、爬行和动画后的头部矩阵，驱动地图组件持有的烟雾实例。
    public static void UpdateForDraw(Pawn pawn, Matrix4x4 headMatrix)
    {
        if (Current.ProgramState != ProgramState.Playing || !WorldRendererUtility.DrawingMap ||
            pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned ||
            Find.CurrentMap != pawn.Map || !ErosionPawnUtility.IsErosionBody(pawn)) return;
        Vector3 position = headMatrix.MultiplyPoint3x4(new Vector3(0, 0, -.08f));
        position.y += .01f;
        pawn.Map.GetComponent<MapComponent_ErosionParticles>().UpdateHead(pawn, position, headMatrix.rotation);
    }
}
