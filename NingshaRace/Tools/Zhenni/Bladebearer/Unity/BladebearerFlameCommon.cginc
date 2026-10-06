#include "UnityCG.cginc"

sampler2D _MainTex, _DisplacementTex;
float4 _MainTex_ST, _Displacement, _Root, _Tip, _NoiseScale, _BreakupScale;
half4 _Color;
float _RootLock, _BendPower, _Speed, _DetailStrength, _Phase, _Additive;
float _FlowAngle;
float _Breakup;
float _BladebearerPreviewEnabled, _BladebearerPreviewTime;
float _PhaseDissolve;

//瞬移与死亡共用固定的轮廓破碎场，火屑从同一位置脱离或聚合。
float phaseField(float2 uv)
{
    float2 noise = tex2Dlod(_DisplacementTex, float4(uv * float2(3.7, 4.9) + float2(0.173, 0.719), 0, 0)).rg;
    float fine = tex2Dlod(_DisplacementTex, float4(uv * 17.3 + 0.41, 0, 0)).r;
    return clamp(noise.r * 0.48 + noise.g * 0.23 + fine * 0.17 + saturate(uv.y) * 0.12, 0.02, 0.98);
}

//扩展网格为置换留出空间，UV 仍以原图画布为基准。
struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
v2f vert(appdata v)
{
    v2f o;
    v.vertex.xyz *= 1.8;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv * 1.8 - 0.4;
    return o;
}

//根部固定，主波动与高梯度二维置换共同拉开火焰轮廓。
float2 displacedUV(float2 sourceUV)
{
    //实体与静止光束无需重复采样置换噪声。
    if (max(abs(_Displacement.x), abs(_Displacement.y)) < 0.000001) return sourceUV;
    float2 axis = _Tip.xy - _Root.xy;
    float lengthSquared = max(dot(axis, axis), 0.00001);
    float2 direction = axis * rsqrt(lengthSquared);
    float along = saturate(dot(sourceUV - _Root.xy, axis) / lengthSquared);
    float weight = pow(smoothstep(_RootLock, 1.0, along), _BendPower);
    //固定范围沿原图主轴保持不动，独立旋转传播方向和二维位移。
    float sinAngle, cosAngle;
    sincos(radians(_FlowAngle), sinAngle, cosAngle);
    float2x2 flowRotation = float2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
    direction = mul(flowRotation, direction);
    float clock = lerp(_Time.y, _BladebearerPreviewTime, _BladebearerPreviewEnabled);
    float time = clock * _Speed + _Phase;
    float2 relative = sourceUV - _Root.xy;
    float across = dot(relative, float2(-direction.y, direction.x));
    float distance = dot(relative, direction);
    float2 noiseUV = float2(_Phase * 0.37 + 0.173, distance * _NoiseScale.y - time);
    float2 coarse = tex2D(_DisplacementTex, noiseUV).rg * 2.0 - 1.0;
    float2 fine = tex2D(_DisplacementTex, noiseUV * 2.07
        + float2(across * _NoiseScale.x + 0.31, 0.67 - time * 0.37)).rg * 2.0 - 1.0;
    float2 displacement = (coarse + fine * _DetailStrength) / (1.0 + _DetailStrength);
    float2 uv = sourceUV + mul(flowRotation, displacement * _Displacement.xy) * weight;
    if (_Breakup > 0.001)
    {
        float inverseLength = rsqrt(lengthSquared);
        float2 breakupUV = float2(across * inverseLength * _BreakupScale.x,
            distance * inverseLength * _BreakupScale.y - time * 2.2);
        breakupUV += float2(_Phase * 0.43, _Phase * 0.19);
        float2 tear = tex2D(_DisplacementTex, breakupUV).rg * 2.0 - 1.0;
        tear = clamp(tear * 2.5, -1.0, 1.0);
        float loose = smoothstep(_RootLock + 0.04, 0.65, along);
        uv += mul(flowRotation, tear * _Displacement.xy) * (_Breakup * loose * 0.65);
    }
    return uv;
}

//颜色与 Alpha 读取相同的置换坐标，破碎不依赖噪声擦除。
half4 sampleFlame(float2 sourceUV)
{
    float2 uv = displacedUV(sourceUV);
    float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
    half4 c = tex2D(_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw) * _Color;
    c.a *= inside;
    if (_PhaseDissolve > 0.00001)
    {
        float field = phaseField(sourceUV);
        float mask = smoothstep(_PhaseDissolve - 0.022, _PhaseDissolve + 0.022, field);
        float edge = (1 - smoothstep(0.018, 0.095, field - _PhaseDissolve)) * saturate(_PhaseDissolve * 18);
        c.rgb = lerp(c.rgb, c.rgb * 0.25 + half3(0.68, 0.22, 1.2), edge);
        c.a *= mask;
    }
    return c;
}

//保留原图颜色与透明边缘，光束图层沿用原画的加色混合。
half4 frag(v2f i) : SV_Target
{
    half4 c = sampleFlame(i.uv);
    return half4(c.rgb * c.a, c.a * (1.0 - _Additive));
}

//只有实体层的足够实心像素写入深度，空白和柔边不遮挡场景。
half4 fragDepth(v2f i) : SV_Target
{
    clip(sampleFlame(i.uv).a - 0.65);
    return 0;
}
