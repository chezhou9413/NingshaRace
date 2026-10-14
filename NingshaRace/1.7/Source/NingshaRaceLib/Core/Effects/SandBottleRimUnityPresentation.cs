using RimUnity;

namespace NingshaRaceLib.Core.Effects;

//沙瓶的表现调整只作用于实例，标准导出资源保持原样。
internal static class SandBottleRimUnityPresentation
{
    //保留原技能的尺寸、密度和透明度调节。
    internal static void Configure(ParticleSystem[] systems)
    {
        foreach (var system in systems)
        {
            var main = system.main;
            main.loop = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSize = Scale(main.startSize, .7f);
            main.startSizeX = Scale(main.startSizeX, .7f);
            main.startSizeY = Scale(main.startSizeY, .7f);
            main.startSizeZ = Scale(main.startSizeZ, .7f);
            system.emission.rateOverTime = Scale(system.emission.rateOverTime, .55f);
            system.emission.rateOverDistance = Scale(system.emission.rateOverDistance, .55f);
            for (int i = 0; i < system.emission.burstCount; i++)
            {
                var burst = system.emission.GetBurst(i);
                burst.count = Scale(burst.count, .65f);
                system.emission.SetBurst(i, burst);
            }
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) continue;
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Alpha"))
            {
                //RimUnity 的 material 属性为实例克隆，并随渲染器释放。
                var material = renderer.material;
                material.SetFloat("_Alpha", material.GetFloat("_Alpha") * .45f);
            }
        }
    }

    //同时处理常量、随机区间与曲线幅度，不改动共享 AnimationCurve。
    private static ParticleSystem.MinMaxCurve Scale(ParticleSystem.MinMaxCurve curve, float factor)
    {
        curve.constantMin *= factor;
        curve.constantMax *= factor;
        curve.curveMultiplier *= factor;
        return curve;
    }
}
