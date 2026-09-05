//职责：提供风沙的噪声扰动、浓度消散与密集颗粒计算，供全幅界面着色器组合。
#ifndef NINGSHA_SANDSTORM_FIELD_INCLUDED
#define NINGSHA_SANDSTORM_FIELD_INCLUDED

sampler2D _NoiseTex;
sampler2D _GrainTex;
float _FlowTime;

//职责：把四尺度噪声合成为连续沙尘体积，避免每个像素重复计算多层随机格点。
float StormNoise(float2 uv)
{
    return dot(tex2D(_NoiseTex, uv), float4(0.48, 0.28, 0.16, 0.08));
}

//职责：以不同速度取样自由散布的细沙，返回细粒、稍大沙粒与微尘，不按格子布置颗粒。
float3 GrainField(float2 uv, float2 wind, float2 warp)
{
    float2 fineUv = uv + wind * 1.35 + warp * 0.32;
    float2 coarseUv = uv + wind * 0.83 - warp * 0.19 + float2(0.41, 0.27);
    //整数坐标变换使两层分布错向叠合，同时维持纹理周期边界连续。
    coarseUv = float2(coarseUv.x + coarseUv.y, coarseUv.y - coarseUv.x);
    float3 fine = tex2D(_GrainTex, fineUv).rgb;
    float coarse = tex2D(_GrainTex, coarseUv).g;
    return float3(fine.r, coarse, fine.b);
}

//职责：组合持续平移与低速涡流，使沙尘团不断翻卷而非平移一张静止噪声图片。
float2 StormWarp(float2 uv)
{
    float2 slowWind = float2(-0.022, 0.014) * _FlowTime;
    float2 broad = tex2D(_NoiseTex, uv + slowWind).rg - 0.5;
    float2 detail = tex2D(_NoiseTex, uv * 2.0 + slowWind * 1.7 + 0.37).ba - 0.5;
    return broad * 0.22 + detail * 0.055;
}

//职责：让不同速度的浓度场互相侵蚀，形成聚拢、破碎和消散的厚重风沙。
float StormDensity(float2 uv, float2 wind, float2 warp, out float billow, out float shreds)
{
    float body = StormNoise(uv + wind + warp);
    float4 breaking = tex2D(_NoiseTex, uv * 2.0 + wind * 1.46 - warp * 0.6 + 0.19);
    billow = smoothstep(0.23, 0.72, body);
    shreds = smoothstep(0.28, 0.73, breaking.b + (breaking.a - 0.5) * 0.32);
    float dissolve = smoothstep(0.22, 0.70, body * 0.65 + breaking.g * 0.35);
    return saturate(billow * (0.40 + dissolve * 0.60) + shreds * 0.17);
}

#endif
