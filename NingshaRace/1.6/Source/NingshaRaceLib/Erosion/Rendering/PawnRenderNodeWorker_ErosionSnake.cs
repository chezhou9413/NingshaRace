using UnityEngine;
using Verse;

namespace NingshaRaceLib.Erosion.Rendering
{
    //类职责：让侵蚀体蛇头围绕贴图蛇尾连接点缩放与摆动。
    public sealed class PawnRenderNodeWorker_ErosionSnake : PawnRenderNodeWorker_Spastic
    {
        //函数职责：把图像从左上起算的归一化连接点转换为网格局部坐标。
        protected override Vector3 PivotFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector2 point = node.Props.drawData.PivotForRot(parms.facing);
            return new Vector3(point.x - 0.5f, 0f, 0.5f - point.y);
        }
    }
}
