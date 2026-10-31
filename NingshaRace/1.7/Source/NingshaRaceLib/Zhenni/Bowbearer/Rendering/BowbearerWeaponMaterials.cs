using NingshaRaceLib.Zhenni.Bladebearer;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Zhenni.Bowbearer;

//螺旋与命中使用独立紫焰材质，地图和预览分别持有实例。
[StaticConstructorOnStartup]
internal static class BowbearerWeaponMaterials
{
    private static Material ghost, previewGhost;

    //连续亮芯和柔光共用同一条螺旋网格。
    internal static Material Trail(BowbearerSettings settings, float time, MaterialPropertyBlock block, bool immediate)
        => Apply(settings.trailColor, settings.trailIntensity, time * settings.trailSpeed,
            settings.distortion, settings.trailGlow, 0, 0, 0, block, immediate);

    //爆焰进度由游戏时钟驱动，编辑器拖动时间轴使用同一套参数。
    internal static Material Impact(BowbearerSettings settings, float time, float progress, float seed,
        MaterialPropertyBlock block, bool immediate)
        => Apply(settings.impactColor, settings.impactIntensity, time, settings.impactDistortion,
            settings.impactGlow, settings.Empowered ? 2 : 1, progress, seed, block, immediate);

    //完整写入本次提交的参数，防止两种效果或多个箭矢共享动态状态。
    private static Material Apply(Color color, float intensity, float time, float distortion, float glow,
        float mode, float progress, float seed, MaterialPropertyBlock block, bool immediate)
    {
        if (ghost == null)
        {
            ghost = BladebearerResources.Create("NingshaRace/Zhenni/Bowbearer/Ghost");
            ghost.renderQueue = BladebearerResources.PhaseColor.renderQueue;
            previewGhost = new Material(ghost);
        }
        Material material = immediate ? previewGhost : ghost;
        if (!ChezhouLib.ALLmap.abDatabase.Texture2dDataBase.TryGetValue(
            BladebearerResources.PackageId + "=>MeleeFlameDisplacement", out Texture2D noise))
            throw new System.InvalidOperationException("挽弓者缺少 MeleeFlameDisplacement 置换图。");
        block.SetTexture("_DisplacementTex", noise); block.SetColor("_Color", color);
        block.SetFloat("_Intensity", intensity);
        block.SetFloat("_RimUnitySrgb_DisplacementTex", 0);
        block.SetFloat("_EffectTime", time); block.SetFloat("_Distortion", distortion);
        block.SetFloat("_Glow", glow); block.SetFloat("_Mode", mode);
        block.SetFloat("_Progress", progress); block.SetFloat("_Seed", seed);
        if (immediate)
        {
            material.SetTexture("_DisplacementTex", noise); material.SetColor("_Color", color);
            material.SetFloat("_Intensity", intensity);
            material.SetFloat("_RimUnitySrgb_DisplacementTex", 0);
            material.SetFloat("_EffectTime", time); material.SetFloat("_Distortion", distortion);
            material.SetFloat("_Glow", glow); material.SetFloat("_Mode", mode);
            material.SetFloat("_Progress", progress); material.SetFloat("_Seed", seed);
        }
        return material;
    }

    //透明紫焰统一提交到现有 RimUnity HDR 泛光层。
    internal static void Draw(Mesh mesh, Matrix4x4 matrix, Material material, bool immediate, MaterialPropertyBlock block)
        => BladebearerRimUnityBloom.Draw(mesh, matrix, material, true, immediate, block);
}
