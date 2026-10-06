using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Erosion.Rendering
{
    //每条蛇独立设置摆幅和相位，侧面使用较小摆幅。
    public sealed class PawnRenderNodeProperties_ErosionSnake : PawnRenderNodeProperties
    {
        public float swayAngle = 8;
        public float swayPeriod = 6;
        public float swayPhase;
        public float sideAngleFactor = 0.65f;

        //使用普通渲染节点，不创建原版随机抽动状态。
        public PawnRenderNodeProperties_ErosionSnake()
        {
            nodeClass = typeof(PawnRenderNode_ErosionSnake);
            workerClass = typeof(PawnRenderNodeWorker_ErosionSnake);
        }

        //拒绝会使摆动时间或连接点失效的配置。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (drawData == null) yield return debugLabel + "：缺少蛇尾连接点 drawData。";
            if (swayPeriod <= 0 || float.IsNaN(swayPeriod) || float.IsInfinity(swayPeriod))
                yield return debugLabel + "：swayPeriod 必须是大于零的有限秒数。";
            if (swayAngle < 0 || swayAngle > 30 || float.IsNaN(swayAngle))
                yield return debugLabel + "：swayAngle 范围为 0 到 30 度。";
            if (sideAngleFactor < 0 || sideAngleFactor > 1 || float.IsNaN(sideAngleFactor))
                yield return debugLabel + "：sideAngleFactor 范围为 0 到 1。";
            if (float.IsNaN(swayPhase) || float.IsInfinity(swayPhase))
                yield return debugLabel + "：swayPhase 必须是有限数字。";
        }
    }
}
