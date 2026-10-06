using System;
using ChezhouLib.ALLmap;
using ChezhouLib.Startup;
using UnityEngine;
using Verse;

namespace NingshaRaceLib.Combat.MeleeAnimation
{
    //近战框架有自己的 Shader 与置换图，不依赖任何怪物的外观组件。
    [StaticConstructorOnStartup]
    internal static class MeleeAnimationResources
    {
        private static readonly Material world, preview, flash, previewFlash;
        private static readonly Texture2D noise;
        //地图与即时预览分别持有材质，参数通过独立属性块提交。
        static MeleeAnimationResources()
        {
            InitiaUnityShaderLord.EnsureInitialized();
            const string package = "chezhou.race.ningsharace";
            Shader shader = abDatabase.GetShader("NingshaRace/Combat/MeleeFlameTrail", package);
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("近战刀光 Shader 无法加载：ningsha_melee.ab。");
            if (!abDatabase.Texture2dDataBase.TryGetValue(package + "=>MeleeFlameDisplacement", out noise))
                throw new InvalidOperationException("近战框架缺少 MeleeFlameDisplacement 置换图。");
            world = new Material(shader) { name = "近战火焰刀光", hideFlags = HideFlags.HideAndDontSave,
                renderQueue = Mathf.Max(MatBases.LightOverlay.renderQueue, MatBases.LightOverlayGravship.renderQueue) + 1 };
            preview = new Material(world);
            Texture2D star = ContentFinder<Texture2D>.Get("Combat/MeleeAnimation/MeleeChargeStar", false);
            if (star == null) throw new InvalidOperationException("近战预警缺少柔光星芒素材：Combat/MeleeAnimation/MeleeChargeStar。");
            star.filterMode = FilterMode.Bilinear; star.wrapMode = TextureWrapMode.Clamp;
            flash = new Material(ShaderDatabase.MoteGlow)
            {
                name = "近战柔光星芒", mainTexture = star, color = Color.white,
                hideFlags = HideFlags.HideAndDontSave, renderQueue = world.renderQueue
            };
            previewFlash = new Material(flash);
        }

        //全部参数每次写入，避免不同单位或不同动作串色。
        internal static Material Apply(MeleeAnimationMove move, float time, Color tint, MaterialPropertyBlock block, bool immediate)
        {
            block.SetTexture("_DisplacementTex", noise);
            block.SetColor("_Color", move.color * tint); block.SetFloat("_SlashTime", time);
            block.SetFloat("_SlashDistortion", move.flameDistortion); block.SetFloat("_Glow", move.bloom);
            if (!immediate) return world;
            preview.SetTexture("_DisplacementTex", noise); preview.SetColor("_Color", move.color * tint);
            preview.SetFloat("_SlashTime", time); preview.SetFloat("_SlashDistortion", move.flameDistortion);
            preview.SetFloat("_Glow", move.bloom);
            return preview;
        }

        //星芒使用独立缓存材质；透明度渐隐保留主体色调，不被刀光置换拉成长条。
        internal static Material Flash(Color color, MaterialPropertyBlock block, bool immediate)
        {
            block.SetColor(ShaderPropertyIDs.Color, color);
            if (!immediate) return flash;
            previewFlash.color = color;
            return previewFlash;
        }
    }
}
