#version 450

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0, std140) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    float _GameTime;
} u;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _AlphaAddTex;

//与原版 TerrainFadeRough 保持一致，边缘来自地形顶点透明度和原版噪声。
float roughFade(vec2 position, float rawAlpha)
{
    float noise = texture(_AlphaAddTex, position * 2.0).r * 0.333
                + texture(_AlphaAddTex, position * 5.0).g * 0.333
                + texture(_AlphaAddTex, position * 10.0).b * 0.333;
    float alpha = (mix(rawAlpha, noise, 0.6) - 0.3) / 0.4;
    return clamp(alpha, max(rawAlpha * 1.5 - 0.5, 0.0), min(rawAlpha * 1.5, 1.0));
}

//连续世界坐标避免逐格相位接缝；游戏暂停时全局时间停止。
void main()
{
    vec2 uv = vec2(vUv.x, -vUv.y) / 16.0;
    float phase = u._GameTime * 0.48;
    vec2 motion = vec2(sin(vUv.y * 0.54 + phase), cos(vUv.x * 0.47 - phase * 0.83)) * 0.0025;
    vec4 surface = texture(_MainTex, uv + motion);
    float pulse = sin(phase + vUv.x * 0.23 + vUv.y * 0.19);
    surface.rgb *= 1.0 + pulse * 0.055;
    vec4 lit = surface * vColor;
    lit.a = roughFade(uv, surface.a * vColor.a);
    outColor = lit * u._Color;
}
