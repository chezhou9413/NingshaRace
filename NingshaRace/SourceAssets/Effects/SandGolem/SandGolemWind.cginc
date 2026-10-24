//左上先剥离，右下最后消失；聚拢使用同一进度的逆向轨迹。
static const float2 SandPeelDirection = float2(0.44, -0.56);
static const float SandPeelStart = 0.035;
static const float SandPeelDuration = 0.68;

//返回贴图位置对应的剥离时刻。
float GetPeelTime(float2 uv)
{
    return SandPeelStart + SandPeelDuration * (dot(uv, SandPeelDirection) + 0.56);
}

//只改变剥离边界附近的透明度，保持其余身体纹理和位置。
float GetRemainingBody(float2 uv, float dissolve)
{
    float breakup = (ValueNoise(uv * 90.0) - 0.5) * 0.018;
    float peel = GetPeelTime(uv) + breakup;
    float edgeWidth = max(fwidth(peel), 0.002);
    return 1.0 - smoothstep(peel - edgeWidth, peel + edgeWidth, dissolve);
}

//由屏幕片元反推碎砂的原位置，使颗粒从身体连续飘走。
float4 GetWindGrainLayer(float2 uv, float dissolve, float radius, float layer, fixed4 tint)
{
    float seed = 13.7 + layer * 31.9;
    float2 velocity = float2(-1.75 + layer * 0.22, 1.45 + layer * 0.38) * radius;
    float2 peelGradient = SandPeelDirection * SandPeelDuration;
    float timeAtUv = dissolve - GetPeelTime(uv);
    //线性剥离时刻与位移共同反解；风向和剥离方向相反，分母始终不小于一。
    float2 sourceUv = uv - velocity * timeAtUv / (1.0 - dot(velocity, peelGradient));
    float scale = max(_ParticleScale * (0.82 + layer * 0.13), 1.0);
    float2 cell = floor(sourceUv * scale);
    float randomSize = Hash21(cell + seed);
    float randomLife = Hash21(cell + float2(seed * 2.11, seed * 0.73));
    float randomKeep = Hash21(cell + float2(seed * 5.23, seed * 6.29));
    float2 jitter = float2(Hash21(cell + float2(seed, 7.1)), Hash21(cell + float2(19.3, seed))) - 0.5;
    float2 sourceCenter = (cell + 0.5 + jitter * _ParticleJitter * 0.4) / scale;
    float age = dissolve - GetPeelTime(sourceCenter);
    float life = lerp(0.15, 0.28, randomLife);
    float visibility = step(0.0, age) * (1.0 - smoothstep(life * 0.35, life, age));
    visibility *= step(1.0 - _ParticleDensity, randomKeep);

    float2 center = sourceCenter + velocity * age;
    float2 local = (uv - center) * scale;
    float2 wind = normalize(float2(-1.75 + layer * 0.22, 1.45 + layer * 0.38));
    float2 alongWind = float2(dot(local, wind), dot(local, float2(-wind.y, wind.x)));
    float halfSize = _ParticleSize * lerp(0.3, 0.55, randomSize);
    //短而不等长的碎砂沿风向排列，保留颗粒之间的空隙。
    float distanceToGrain = max(abs(alongWind.x) / lerp(1.0, 1.8, randomLife), abs(alongWind.y));
    float softness = max(fwidth(distanceToGrain), 0.025);
    float mask = 1.0 - smoothstep(halfSize - softness, halfSize + softness, distanceToGrain);
    fixed4 source = SamplePawn(sourceCenter) * _Color * tint;
    float alpha = source.a * mask * visibility;
    float grain = lerp(0.35, 0.95, randomSize);
    fixed3 color = GetSandColor(GetLuminance(source.rgb), grain);
    color = lerp(color, color * source.rgb, saturate(_OriginalTint));
    color *= _Brightness * lerp(0.9, 1.25, randomLife);
    return float4(color * alpha, alpha);
}

//三层略有不同的速度让砂流错落，同时保持确定的聚散轨迹。
float4 GetWindGrains(float2 uv, float dissolve, float radius, fixed4 tint)
{
    float4 result = 0;
    [unroll]
    for (int layer = 0; layer < 3; layer++)
    {
        float4 grain = GetWindGrainLayer(uv, dissolve, radius, layer, tint);
        result.rgb += grain.rgb * (1.0 - result.a);
        result.a += grain.a * (1.0 - result.a);
    }
    return result;
}
