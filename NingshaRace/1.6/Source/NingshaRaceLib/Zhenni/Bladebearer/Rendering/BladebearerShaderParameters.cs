using System.Collections.Generic;
using UnityEngine;

namespace NingshaRaceLib.Zhenni.Bladebearer
{
    //地图写属性块；原版 DrawMeshNow 不使用属性块，因此即时预览同时写专用材质。
    internal struct BladebearerShaderParameters
    {
        private static readonly Dictionary<string, int> ids = new Dictionary<string, int>();
        private MaterialPropertyBlock block;
        private Material immediate;

        //完整写入当前部件参数，不依赖上一次绘制遗留状态。
        internal static void Apply(BladebearerPart p, float time, Color tint, MaterialPropertyBlock block, Material immediate)
        {
            ApplyStatic(p, time, tint, block, immediate);
            var w = new BladebearerShaderParameters { block = block, immediate = immediate };
            w.Texture("_DisplacementTex", BladebearerResources.Noise(p.displacementTexture));
            //粒子由独立网格绘制，渲染树占位请求不套用火焰位移。
            w.Pair("_Displacement", p.particle ? Vector2.zero : p.displacement); w.Pair("_Root", p.root); w.Pair("_Tip", p.tip);
            w.Pair("_NoiseScale", p.noiseScale); w.Pair("_BreakupScale", p.breakupScale);
            w.Float("_RootLock", p.rootLock); w.Float("_BendPower", p.bendPower);
            w.Float("_Speed", p.speed); w.Float("_DetailStrength", p.detailStrength); w.Float("_Breakup", p.particle ? 0 : p.breakup);
            w.Float("_FlowAngle", p.flowAngle);
            w.Float("_Phase", p.phase);
            w.Float("_BladebearerPreviewEnabled", 1); w.Float("_BladebearerPreviewTime", time);
            w.Float("_PhaseDissolve", 0);
        }

        //聚合和消散只在独立事件期间裁切轮廓，常态火焰仍只使用 RG 置换。
        internal static void ApplyPhase(float dissolve, MaterialPropertyBlock block)
        { block.SetFloat("_PhaseDissolve", dissolve); }

        //星光只调节 RGB，保留透明轮廓；本体与辉光捕获使用同一时间曲线。
        private static void ApplyStatic(BladebearerPart p, float time, Color tint, MaterialPropertyBlock block, Material immediate)
        {
            var w = new BladebearerShaderParameters { block = block, immediate = immediate };
            w.Texture("_MainTex", BladebearerResources.Texture(p.texture));
            w.Vector("_MainTex_ST", new Vector4(1, 1, 0, 0));
            Color color = p.color * tint;
            if (p.additive > 0)
            {
                float intensity = p.lightIntensity;
                if (!p.flame && !p.particle && p.lightFlickerStrength > 0)
                {
                    float cycle = time * p.lightFlickerFrequency * (2f * Mathf.PI) + p.phase;
                    //叠加两种连续节奏，幅度受限，避免整层熄灭或逐帧随机跳变。
                    float flicker = 0.7f * Mathf.Sin(cycle) + 0.3f * Mathf.Sin(cycle * 2.37f + 0.8f);
                    intensity *= 1f + p.lightFlickerStrength * flicker;
                }
                color.r *= intensity; color.g *= intensity; color.b *= intensity;
            }
            w.Vector("_Color", color); w.Float("_Additive", p.additive);
        }

        //动态粒子由顶点颜色携带寿命透明度，不读取静态粒子底图。
        internal static void ApplyParticles(BladebearerPart p, Color tint, MaterialPropertyBlock block, Material immediate)
        {
            var w = new BladebearerShaderParameters { block = block, immediate = immediate };
            w.Float("_BloomCapture", 0);
            w.Vector("_Color", p.color * tint); w.Float("_Additive", p.additive);
        }

        //缓存原生 Shader 属性编号。
        private static int Id(string name)
        {
            if (!ids.TryGetValue(name, out int id)) { id = Shader.PropertyToID(name); ids.Add(name, id); }
            return id;
        }
        //写标量。
        private void Float(string name, float value)
        { int id = Id(name); block.SetFloat(id, value); if (immediate != null) immediate.SetFloat(id, value); }
        //写向量或颜色。
        private void Vector(string name, Vector4 value)
        { int id = Id(name); block.SetVector(id, value); if (immediate != null) immediate.SetVector(id, value); }
        //把二维参数按 Shader 的 XY 分量传入。
        private void Pair(string name, Vector2 value) => Vector(name, new Vector4(value.x, value.y, 0, 0));
        //写贴图，不改写资源的导入状态。
        private void Texture(string name, Texture value)
        { int id = Id(name); block.SetTexture(id, value); if (immediate != null) immediate.SetTexture(id, value); }
    }
}
