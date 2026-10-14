#version 450
vec3 RU_Decode(vec3 c) {
    return mix(pow(max((c + 0.055) / 1.055, vec3(0.0)), vec3(2.4)), c / 12.92, lessThanEqual(c, vec3(0.04045)));
}
vec3 RU_Encode(vec3 c) {
    return mix(1.055 * pow(max(c, vec3(0.0)), vec3(1.0 / 2.4)) - 0.055, c * 12.92, lessThanEqual(c, vec3(0.0031308)));
}
vec4 RU_Linear(vec4 c) { return vec4(RU_Decode(c.rgb), c.a); }
vec3 RU_Linear(vec3 c) { return RU_Decode(c); }
vec2 RU_Linear(vec2 c) { return RU_Decode(vec3(c, 0.0)).xy; }
float RU_Linear(float c) { return RU_Decode(vec3(c)).x; }
vec4 RU_SampleLinear(vec4 c, float srgb) { return srgb > 0.5 ? RU_Linear(c) : c; }
#extension GL_EXT_control_flow_attributes : require


#line 122 0
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 124



#line 56
struct RU_MaterialData_0
{
    float _AutoFitMask; // @default = 1
    vec4 _BubbleColor; // @default = vec4(1, 1, 1, 0.45) @color
    float _BubbleDensity; // @default = 24
    float _BubbleSize; // @default = 0.055
    float _BubbleSpeed; // @default = 0.35
    float _BubbleStrength; // @default = 0.35
    vec4 _EdgeFlowDirection; // @default = vec4(0.25, 1, 0, 0)
    float _EdgeFlowScale; // @default = 8
    float _EdgeFlowSpeed; // @default = 0.55
    float _EdgeFlowStrength; // @default = 0.35
    float _EdgeFlowWidth; // @default = 0.035
    float _FillAmount; // @default = 0.5
    float _FillCurve; // @default = 1.8
    float _FillSoftness; // @default = 0.02
    float _LiquidAngle; // @default = -10
    float _LiquidCenterOffsetX; // @default = 0
    float _LiquidCenterOffsetY; // @default = 0
    vec4 _LiquidColor; // @default = vec4(0.15, 0.65, 1, 0.85) @color
    float _LiquidMaxHeight; // @default = 1.35
    float _LiquidMaxWidth; // @default = 1.35
    float _LiquidMinHeight; // @default = 0.12
    float _LiquidMinWidth; // @default = 0.18
    float _LiquidVerticalShift; // @default = 0.12
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseContrast; // @default = 1.4
    vec4 _NoiseDirection; // @default = vec4(1, 0.35, 0, 0)
    float _NoiseScale; // @default = 18
    float _NoiseSpeed; // @default = 0.35
    float _NoiseStrength; // @default = 0.22
    vec4 _NoiseTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _Time;
};


#line 89
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AutoFitMask; // @default = 1
    vec4 _BubbleColor; // @default = vec4(1, 1, 1, 0.45) @color
    float _BubbleDensity; // @default = 24
    float _BubbleSize; // @default = 0.055
    float _BubbleSpeed; // @default = 0.35
    float _BubbleStrength; // @default = 0.35
    vec4 _EdgeFlowDirection; // @default = vec4(0.25, 1, 0, 0)
    float _EdgeFlowScale; // @default = 8
    float _EdgeFlowSpeed; // @default = 0.55
    float _EdgeFlowStrength; // @default = 0.35
    float _EdgeFlowWidth; // @default = 0.035
    float _FillAmount; // @default = 0.5
    float _FillCurve; // @default = 1.8
    float _FillSoftness; // @default = 0.02
    float _LiquidAngle; // @default = -10
    float _LiquidCenterOffsetX; // @default = 0
    float _LiquidCenterOffsetY; // @default = 0
    vec4 _LiquidColor; // @default = vec4(0.15, 0.65, 1, 0.85) @color
    float _LiquidMaxHeight; // @default = 1.35
    float _LiquidMaxWidth; // @default = 1.35
    float _LiquidMinHeight; // @default = 0.12
    float _LiquidMinWidth; // @default = 0.18
    float _LiquidVerticalShift; // @default = 0.12
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseContrast; // @default = 1.4
    vec4 _NoiseDirection; // @default = vec4(1, 0.35, 0, 0)
    float _NoiseScale; // @default = 18
    float _NoiseSpeed; // @default = 0.35
    float _NoiseStrength; // @default = 0.22
    vec4 _NoiseTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _Time;
    float _RimUnitySrgb_MainTex; // @default = 1
    float _RimUnitySrgb_NoiseTex; // @default = 1
}RU_Material_0;

#line 133
layout(binding = 3, set = 2)
uniform sampler2D _NoiseTex; // @default = gray


#line 135



#line 9392 1
void clip_0(float x_0)
{

#line 9398
    if(x_0 < 0.0)
    {

#line 9398
        discard;

#line 9398
    }

    return;
}


#line 14946
float saturate_0(float x_1)
{

#line 14954
    return clamp(x_1, 0.0, 1.0);
}


#line 126 0
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 126
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 145
float GetLiquidGrowth_0()
{

#line 145
    return pow(saturate_0(RU_Material_0._FillAmount), mix(1.0, max(RU_Material_0._FillCurve, 0.00009999999747379), 0.55000001192092896));
}


#line 145
vec4 GetMaskData_0()
{

#line 145
    const vec2 _S1 = vec2(1.0, 1.0);

#line 145
    const vec2 _S2 = vec2(0.0, 0.0);

#line 145
    vec2 minUv_0 = _S1;

#line 145
    vec2 maxUv_0 = _S2;

#line 145
    float hasMask_0 = 0.0;

#line 145
    int y_0 = 0;

#line 145
    [[unroll]]
    for(;;)
    {

#line 145
        if(y_0 < 8)
        {
        }
        else
        {

#line 145
            break;
        }

#line 145
        int x_2 = 0;

#line 145
        [[unroll]]
        for(;;)
        {

#line 145
            if(x_2 < 8)
            {
            }
            else
            {

#line 145
                break;
            }

#line 145
            vec2 sampleUv_0 = (vec2(float(x_2), float(y_0)) + 0.5) * 0.125;

#line 145
            float sampleMask_0 = step(0.00999999977648258, RU_tex2D_MainTex_0(sampleUv_0).w);

#line 145
            vec2 _S3 = vec2(sampleMask_0);

#line 145
            vec2 _S4 = mix(minUv_0, min(minUv_0, sampleUv_0), _S3);

#line 145
            vec2 _S5 = mix(maxUv_0, max(maxUv_0, sampleUv_0), _S3);

#line 145
            float _S6 = max(hasMask_0, sampleMask_0);

#line 145
            int _S7 = x_2 + 1;

#line 145
            minUv_0 = _S4;

#line 145
            maxUv_0 = _S5;

#line 145
            hasMask_0 = _S6;

#line 145
            x_2 = _S7;

#line 145
        }

#line 145
        y_0 = y_0 + 1;

#line 145
    }

#line 145
    return vec4(mix(vec2(0.5, 0.5), (minUv_0 + maxUv_0) * 0.5, vec2(hasMask_0 * saturate_0(RU_Material_0._AutoFitMask))), mix(_S1, max(maxUv_0 - minUv_0, vec2(0.00999999977648258, 0.00999999977648258)), vec2(hasMask_0 * saturate_0(RU_Material_0._AutoFitMask))));
}


#line 145
float GetEdgeJitter_0(vec2 uv_1, float growth_0, vec4 maskData_0)
{

#line 145
    float swayPhase_0 = RU_Material_0._Time.y * RU_Material_0._EdgeFlowSpeed;

#line 145
    float _S8 = ((uv_1 - maskData_0.xy) / max(maskData_0.zw, vec2(0.00009999999747379, 0.00009999999747379))).x;

#line 145
    return (_S8 * (sin(swayPhase_0) * RU_Material_0._EdgeFlowStrength) * RU_Material_0._EdgeFlowDirection.x + sin(_S8 * RU_Material_0._EdgeFlowScale + swayPhase_0 * 1.70000004768371582) * RU_Material_0._EdgeFlowWidth * RU_Material_0._EdgeFlowDirection.y) * (1.0 - smoothstep(0.98500001430511475, 1.0, saturate_0(RU_Material_0._FillAmount)));
}


#line 145
vec2 GetLiquidCenterOffset_0(float growth_1, vec4 maskData_1)
{

#line 145
    return vec2(RU_Material_0._LiquidCenterOffsetX, RU_Material_0._LiquidCenterOffsetY) * maskData_1.zw * (1.0 - smoothstep(0.18000000715255737, 0.92000001668930054, growth_1));
}


#line 145
vec2 GetLiquidCenter_0(float growth_2, vec4 maskData_2)
{

#line 145
    return maskData_2.xy + GetLiquidCenterOffset_0(growth_2, maskData_2) + vec2(0.0, RU_Material_0._LiquidVerticalShift * maskData_2.zw.y * (growth_2 - 0.5) * (1.0 - smoothstep(0.69999998807907104, 1.0, growth_2)));
}


#line 145
vec2 GetMaskLocalUv_0(vec2 uv_2, float growth_3, vec4 maskData_3)
{

#line 145
    vec2 local_0 = (uv_2 - GetLiquidCenter_0(growth_3, maskData_3)) / max(maskData_3.zw * 0.5, vec2(0.00009999999747379, 0.00009999999747379));

#line 145
    float angle_0 = - RU_Material_0._LiquidAngle * 0.01745329238474369;

#line 145
    float angleSin_0 = sin(angle_0);

#line 145
    float angleCos_0 = cos(angle_0);

#line 145
    float _S9 = local_0.x;

#line 145
    float _S10 = local_0.y;

#line 145
    return vec2(_S9 * angleCos_0 - _S10 * angleSin_0, _S9 * angleSin_0 + _S10 * angleCos_0);
}


#line 145
float GetLiquidBodyMask_0(vec2 uv_3, float growth_4, vec4 maskData_4, float edgeJitter_0)
{

#line 145
    float amount_0 = saturate_0(RU_Material_0._FillAmount);

#line 145
    vec2 local_1 = GetMaskLocalUv_0(uv_3, growth_4, maskData_4);

#line 145
    float normalizedX_0 = local_1.x / max(mix(RU_Material_0._LiquidMinWidth, RU_Material_0._LiquidMaxWidth, smoothstep(0.0, 1.0, growth_4)), 0.00009999999747379);

#line 145
    float curvedSurfaceY_0 = mix(-1.35000002384185791, 1.22000002861022949, pow(amount_0, 1.35000002384185791)) + sqrt(saturate_0(1.0 - normalizedX_0 * normalizedX_0)) * mix(RU_Material_0._LiquidMinHeight, RU_Material_0._LiquidMaxHeight, smoothstep(0.0, 1.0, pow(growth_4, 0.85000002384185791))) * 0.55000001192092896 + edgeJitter_0;

#line 145
    return mix((1.0 - smoothstep(curvedSurfaceY_0, curvedSurfaceY_0 + max(RU_Material_0._FillSoftness, 0.00009999999747379), local_1.y)) * mix(1.0 - smoothstep(0.98000001907348633, 1.01999998092651367, abs(normalizedX_0)), 1.0, smoothstep(0.44999998807907104, 0.89999997615814209, amount_0)), 1.0, smoothstep(0.98500001430511475, 1.0, amount_0));
}


#line 137
vec4 RU_tex2D_NoiseTex_0(vec2 uv_4)
{

#line 137
    return (RU_SampleLinear(texture(_NoiseTex, (vec2(uv_4.x, 1.0 - uv_4.y))), RU_Material_0._RimUnitySrgb_NoiseTex));
}


#line 145
float SampleFlowNoise_0(vec2 uv_5, float scale_0, float speed_0, vec2 direction_0)
{

#line 145
    float centeredNoise_0 = (RU_tex2D_NoiseTex_0(uv_5 * scale_0 * RU_Material_0._NoiseTex_ST.zwxy.xy + RU_Material_0._NoiseTex_ST.zwxy.zw + normalize(direction_0 + vec2(0.00009999999747379, 0.00009999999747379)) * (RU_Material_0._Time.y * speed_0)).x - 0.5) * 2.0;

#line 145
    return float((int(sign((centeredNoise_0))))) * pow(abs(centeredNoise_0), max(RU_Material_0._NoiseContrast, 0.00009999999747379));
}


#line 145
float GetFlowNoise_0(vec2 uv_6, vec4 maskData_5, float fillMask_0)
{

#line 145
    return SampleFlowNoise_0((uv_6 - maskData_5.xy) / max(maskData_5.zw, vec2(0.00009999999747379, 0.00009999999747379)), RU_Material_0._NoiseScale, RU_Material_0._NoiseSpeed, RU_Material_0._NoiseDirection.xy) * RU_Material_0._NoiseStrength * fillMask_0;
}


#line 145
vec2 GetLiquidRadii_0(float growth_5, vec4 maskData_6)
{

#line 145
    vec2 maskSize_0 = maskData_6.zw;

#line 145
    return vec2(max(maskSize_0.x * mix(RU_Material_0._LiquidMinWidth, RU_Material_0._LiquidMaxWidth, smoothstep(0.0, 1.0, growth_5)) * 0.5, 0.00009999999747379), max(maskSize_0.y * mix(RU_Material_0._LiquidMinHeight, RU_Material_0._LiquidMaxHeight, smoothstep(0.0, 1.0, pow(growth_5, 0.85000002384185791))) * 0.5, 0.00009999999747379));
}


#line 145
float Hash21_0(vec2 value_0)
{

#line 145
    return fract(sin(dot(value_0, vec2(127.09999847412109375, 311.70001220703125))) * 43758.546875);
}


#line 145
float GetBubbleMask_0(vec2 uv_7, float growth_6, vec4 maskData_7, float fillMask_1)
{

#line 145
    float angle_1 = - RU_Material_0._LiquidAngle * 0.01745329238474369;

#line 145
    float angleSin_1 = sin(angle_1);

#line 145
    float angleCos_1 = cos(angle_1);

#line 145
    vec2 offset_0 = uv_7 - GetLiquidCenter_0(growth_6, maskData_7);

#line 145
    float _S11 = offset_0.x;

#line 145
    float _S12 = offset_0.y;

#line 145
    vec2 bubbleUv_0 = vec2(_S11 * angleCos_1 - _S12 * angleSin_1, _S11 * angleSin_1 + _S12 * angleCos_1) / max(GetLiquidRadii_0(growth_6, maskData_7), vec2(0.00009999999747379, 0.00009999999747379)) * 0.5 + 0.5;

#line 145
    bubbleUv_0[1] = bubbleUv_0[1] + RU_Material_0._Time.y * RU_Material_0._BubbleSpeed * 0.07999999821186066;

#line 145
    vec2 _S13 = bubbleUv_0 * max(RU_Material_0._BubbleDensity, 1.0);

#line 145
    bubbleUv_0 = _S13;

#line 145
    vec2 cell_0 = floor(_S13);

#line 145
    float randomValue_0 = Hash21_0(cell_0);

#line 145
    float bubbleRadius_0 = RU_Material_0._BubbleSize * mix(0.44999998807907104, 1.25, randomValue_0);

#line 145
    return (1.0 - smoothstep(bubbleRadius_0, bubbleRadius_0 + 0.02500000037252903, length(fract(_S13) - vec2(Hash21_0(cell_0 + 13.69999980926513672), Hash21_0(cell_0 + 41.29999923706054688))))) * step(0.62000000476837158, randomValue_0) * fillMask_1 * RU_Material_0._BubbleStrength;
}


#line 145
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 145
layout(location = 5) in vec2 i_rangeUv_0; // @vertex TexCoord1 optional


#line 145
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 145
void RU_SourceMain()
{

#line 145
    float growth_7 = GetLiquidGrowth_0();

#line 145
    vec4 maskData_8 = GetMaskData_0();

#line 145
    float fillMask_2 = GetLiquidBodyMask_0(i_rangeUv_0, growth_7, maskData_8, GetEdgeJitter_0(i_rangeUv_0, growth_7, maskData_8)) * RU_tex2D_MainTex_0(i_rangeUv_0).w;

#line 145
    float flowNoise_0 = GetFlowNoise_0(i_rangeUv_0, maskData_8, fillMask_2);

#line 145
    float bubbleMask_0 = GetBubbleMask_0(i_rangeUv_0, growth_7, maskData_8, fillMask_2);

#line 145
    float visibleMask_0 = saturate_0(fillMask_2);

#line 145
    clip_0(visibleMask_0 - 0.00100000004749745);

#line 145
    vec4 _S14 = RU_Linear(RU_Material_0._LiquidColor) * i_color_0;

#line 145
    vec4 liquid_0 = _S14;

#line 145
    liquid_0.xyz = _S14.xyz * (1.0 + flowNoise_0);

#line 145
    liquid_0.xyz = mix(liquid_0.xyz, RU_Linear(RU_Material_0._BubbleColor).xyz, vec3(saturate_0(bubbleMask_0 * RU_Linear(RU_Material_0._BubbleColor).w)));

#line 145
    liquid_0[3] = liquid_0[3] * visibleMask_0;

#line 145
    entryPointParam_frag_0 = liquid_0;

#line 145
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
