using System.Collections.Generic;
using Verse;

namespace NingshaRaceLib.Erosion.Rendering
{
    //每条蛇独立设置摆幅和相位，侧面使用较小摆幅。
    public sealed class PawnRenderNodeProperties_ErosionSnake : PawnRenderNodeProperties
    {
        public const float MinimumLayer = -1000;
        public const float MaximumLayer = 1000;
        public float swayAngle = 8;
        public float swayPeriod = 6;
        public float swayPhase;
        public float sideAngleFactor = 0.65f;
        public List<ErosionSnakeSway> swayByFacing;

        //未单独设置的方向沿用节点原有摆动参数。
        public ErosionSnakeSway SwayFor(Rot4 facing)
            => swayByFacing?.Find(value => value.facing == facing);

        //编辑时复制节点属性，绘制数据与动画列表由草稿独立创建。
        internal PawnRenderNodeProperties_ErosionSnake CopyForEditor()
            => (PawnRenderNodeProperties_ErosionSnake)MemberwiseClone();

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
            else
            {
                foreach (Rot4 facing in Rot4.AllRotations)
                {
                    float layer = drawData.LayerForRot(facing, baseLayer);
                    if (float.IsNaN(layer) || layer < MinimumLayer || layer > MaximumLayer)
                        yield return debugLabel + "：图层必须在 -1000～1000 之间。";
                }
            }
            if (swayPeriod <= 0 || float.IsNaN(swayPeriod) || float.IsInfinity(swayPeriod))
                yield return debugLabel + "：swayPeriod 必须是大于零的有限秒数。";
            if (swayAngle < 0 || swayAngle > 30 || float.IsNaN(swayAngle))
                yield return debugLabel + "：swayAngle 范围为 0 到 30 度。";
            if (sideAngleFactor < 0 || sideAngleFactor > 1 || float.IsNaN(sideAngleFactor))
                yield return debugLabel + "：sideAngleFactor 范围为 0 到 1。";
            if (float.IsNaN(swayPhase) || float.IsInfinity(swayPhase))
                yield return debugLabel + "：swayPhase 必须是有限数字。";
            if (swayByFacing == null) yield break;
            var seen = new HashSet<int>();
            foreach (ErosionSnakeSway sway in swayByFacing)
            {
                foreach (string error in sway.Errors()) yield return debugLabel + "：" + error;
                if (!seen.Add(sway.facing.AsInt)) yield return debugLabel + "：摆动朝向重复。";
            }
        }
    }
}
