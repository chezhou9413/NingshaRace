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


#line 56 0
struct RU_MaterialData_0
{
    float _AutoFitBox; // @default = 1
    float _BoundaryWarp; // @default = 0.12
    float _BoxPadding; // @default = 0.08
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    vec4 _ColorTwo; // @default = vec4(1, 1, 1, 1) @color
    float _ErosionSoftness; // @default = 0.075
    float _ErosionThreshold; // @default = 0.46
    vec4 _FlowSpeed; // @default = vec4(0.13, 0.28, 0, 0)
    vec4 _FogColor; // @default = vec4(0.005, 0.008, 0.012, 0.92) @color
    float _FogDensity; // @default = 0.9
    float _FogStartY; // @default = 0.5
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    vec4 _MaskTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseDetail; // @default = 0.72
    float _NoiseScale; // @default = 8
    float _PixelAlphaThreshold; // @default = 0.05
    float _PulseStrength; // @default = 0.12
    float _ShapeSoftness; // @default = 0.035
    vec4 _Time;
    float _UseMask; // @default = 0
    float _WarpStrength; // @default = 0.32
};


#line 80
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AutoFitBox; // @default = 1
    float _BoundaryWarp; // @default = 0.12
    float _BoxPadding; // @default = 0.08
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    vec4 _ColorTwo; // @default = vec4(1, 1, 1, 1) @color
    float _ErosionSoftness; // @default = 0.075
    float _ErosionThreshold; // @default = 0.46
    vec4 _FlowSpeed; // @default = vec4(0.13, 0.28, 0, 0)
    vec4 _FogColor; // @default = vec4(0.005, 0.008, 0.012, 0.92) @color
    float _FogDensity; // @default = 0.9
    float _FogStartY; // @default = 0.5
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    vec4 _MaskTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseDetail; // @default = 0.72
    float _NoiseScale; // @default = 8
    float _PixelAlphaThreshold; // @default = 0.05
    float _PulseStrength; // @default = 0.12
    float _ShapeSoftness; // @default = 0.035
    vec4 _Time;
    float _UseMask; // @default = 0
    float _WarpStrength; // @default = 0.32
    float _RimUnitySrgb_MainTex; // @default = 1
    float _RimUnitySrgb_MaskTex; // @default = 1
}RU_Material_0;

#line 104
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 106



#line 115
layout(binding = 3, set = 2)
uniform sampler2D _MaskTex; // @default = black


#line 117



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


#line 14961
vec2 saturate_1(vec2 x_2)
{

#line 14969
    return clamp(x_2, vec2(0.0), vec2(1.0));
}


#line 127 0
float GetUvBoundsMask_0(vec2 uv_0)
{

#line 127
    float _S1 = uv_0.x;

#line 127
    float _S2 = uv_0.y;

#line 127
    return step(0.0, _S1) * step(_S1, 1.0) * step(0.0, _S2) * step(_S2, 1.0);
}


#line 108
vec4 RU_tex2D_MainTex_0(vec2 uv_1)
{

#line 108
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_1.x, 1.0 - uv_1.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 119
vec4 RU_tex2D_MaskTex_0(vec2 uv_2)
{

#line 119
    return (RU_SampleLinear(texture(_MaskTex, (vec2(uv_2.x, 1.0 - uv_2.y))), RU_Material_0._RimUnitySrgb_MaskTex));
}


#line 127
vec4 GetPixelBox_0()
{

#line 127
    const vec2 _S3 = vec2(1.0, 1.0);

#line 127
    const vec2 _S4 = vec2(0.0, 0.0);

#line 127
    vec2 minUv_0 = _S3;

#line 127
    vec2 maxUv_0 = _S4;

#line 127
    float hasPixels_0 = 0.0;

#line 127
    int y_0 = 0;

#line 127
    [[unroll]]
    for(;;)
    {

#line 127
        if(y_0 < 8)
        {
        }
        else
        {

#line 127
            break;
        }

#line 127
        int x_3 = 0;

#line 127
        [[unroll]]
        for(;;)
        {

#line 127
            if(x_3 < 8)
            {
            }
            else
            {

#line 127
                break;
            }

#line 127
            vec2 sampleUv_0 = (vec2(float(x_3), float(y_0)) + 0.5) * 0.125;

#line 127
            float isPixel_0 = step(RU_Material_0._PixelAlphaThreshold, RU_tex2D_MainTex_0(sampleUv_0.xy * RU_Material_0._MainTex_ST.zwxy.xy + RU_Material_0._MainTex_ST.zwxy.zw).w);

#line 127
            vec2 _S5 = vec2(isPixel_0);

#line 127
            vec2 _S6 = mix(minUv_0, min(minUv_0, sampleUv_0), _S5);

#line 127
            vec2 _S7 = mix(maxUv_0, max(maxUv_0, sampleUv_0), _S5);

#line 127
            float _S8 = max(hasPixels_0, isPixel_0);

#line 127
            int _S9 = x_3 + 1;

#line 127
            minUv_0 = _S6;

#line 127
            maxUv_0 = _S7;

#line 127
            hasPixels_0 = _S8;

#line 127
            x_3 = _S9;

#line 127
        }

#line 127
        y_0 = y_0 + 1;

#line 127
    }

#line 127
    vec2 logicalTexturePixels_0 = max(abs(RU_Material_0._MainTex_ST.zwxy.xy) * RU_Material_0._MainTex_TexelSize.zw, _S3);

#line 127
    vec2 sampledPixels_0 = max(maxUv_0 - minUv_0 + 0.125, vec2(0.125, 0.125)) * logicalTexturePixels_0;

#line 127
    vec2 _S10 = vec2(hasPixels_0 * saturate_0(RU_Material_0._AutoFitBox));

#line 127
    return vec4(mix(vec2(0.5, 0.5), (minUv_0 + maxUv_0) * 0.5, _S10), max(mix(_S3, max(sampledPixels_0.x, sampledPixels_0.y) / logicalTexturePixels_0 * (1.0 + max(RU_Material_0._BoxPadding, 0.0) * 2.0), _S10), vec2(0.00009999999747379, 0.00009999999747379)));
}


#line 127
float Hash21_0(vec2 value_0)
{

#line 127
    vec2 _S11 = fract(value_0 * vec2(123.339996337890625, 456.209991455078125));

#line 127
    vec2 _S12 = _S11 + dot(_S11, _S11 + 45.31999969482421875);

#line 127
    return fract(_S12.x * _S12.y);
}


#line 127
float ValueNoise_0(vec2 uv_3)
{

#line 127
    vec2 cell_0 = floor(uv_3);

#line 127
    vec2 local_0 = fract(uv_3);

#line 127
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 127
    float _S13 = blend_0.x;

#line 127
    return mix(mix(Hash21_0(cell_0), Hash21_0(cell_0 + vec2(1.0, 0.0)), _S13), mix(Hash21_0(cell_0 + vec2(0.0, 1.0)), Hash21_0(cell_0 + vec2(1.0, 1.0)), _S13), blend_0.y);
}


#line 127
float GetFlowNoise_0(vec2 boxUv_0)
{

#line 127
    vec2 flow_0 = RU_Material_0._FlowSpeed.xy * RU_Material_0._Time.y;

#line 127
    vec2 warpUv_0 = boxUv_0 * (RU_Material_0._NoiseScale * 0.44999998807907104) + flow_0 * 0.62999999523162842;

#line 127
    vec2 warp_0;

#line 127
    warp_0[0] = ValueNoise_0(warpUv_0 + vec2(17.29999923706054688, 41.70000076293945312));

#line 127
    warp_0[1] = ValueNoise_0(warpUv_0 - vec2(29.10000038146972656, 11.60000038146972656));

#line 127
    vec2 _S14 = (warp_0 - 0.5) * RU_Material_0._WarpStrength * 3.0;

#line 127
    warp_0 = _S14;

#line 127
    vec2 noiseUv_0 = boxUv_0 * max(RU_Material_0._NoiseScale, 0.00100000004749745) + flow_0 + _S14;

#line 127
    float coarse_0 = ValueNoise_0(noiseUv_0);

#line 127
    return mix(coarse_0, coarse_0 * 0.56000000238418579 + ValueNoise_0(noiseUv_0 * 2.02999997138977051 - flow_0 * 0.4699999988079071) * 0.28999999165534973 + ValueNoise_0(noiseUv_0 * 4.1100001335144043 + flow_0 * 0.28999999165534973) * 0.15000000596046448, saturate_0(RU_Material_0._NoiseDetail));
}


#line 127
float GetUpperFaceMask_0(vec2 boxUv_1, float flowNoise_0)
{

#line 127
    float _S15 = max(RU_Material_0._ShapeSoftness, 0.00100000004749745);

#line 127
    float boundary_0 = RU_Material_0._FogStartY + (flowNoise_0 - 0.5) * RU_Material_0._BoundaryWarp;

#line 127
    return smoothstep(boundary_0 - _S15, boundary_0 + _S15, boxUv_1.y);
}


#line 127
float GetErosionMask_0(float flowNoise_1, float envelope_0)
{

#line 127
    float _S16 = max(RU_Material_0._ErosionSoftness, 0.00100000004749745);

#line 127
    return smoothstep(RU_Material_0._ErosionThreshold - _S16, RU_Material_0._ErosionThreshold + _S16, flowNoise_1 + envelope_0 * 0.20000000298023224 + sin(RU_Material_0._Time.y * 1.37000000476837158 + flowNoise_1 * 6.28318548202514648) * RU_Material_0._PulseStrength);
}


#line 127
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 127
layout(location = 1) in vec2 i_effectUv_0; // @vertex TexCoord0 optional


#line 127
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 127
void RU_SourceMain()
{

#line 127
    float sourceBounds_0 = GetUvBoundsMask_0(i_effectUv_0);

#line 127
    vec4 _S17 = RU_tex2D_MainTex_0(saturate_1(i_effectUv_0).xy * RU_Material_0._MainTex_ST.zwxy.xy + RU_Material_0._MainTex_ST.zwxy.zw);

#line 127
    vec4 source_0 = _S17;

#line 127
    vec2 mask_0 = RU_tex2D_MaskTex_0(saturate_1(i_effectUv_0).xy * RU_Material_0._MaskTex_ST.zwxy.xy + RU_Material_0._MaskTex_ST.zwxy.zw).xy;

#line 127
    source_0.xyz = _S17.xyz * (mix(RU_Linear(RU_Material_0._Color).xyz, mix(mix(vec3(1.0, 1.0, 1.0), RU_Linear(RU_Material_0._Color).xyz, vec3(mask_0.x)), RU_Linear(RU_Material_0._ColorTwo).xyz, vec3(mask_0.y)), vec3(saturate_0(RU_Material_0._UseMask))) * i_color_0.xyz);

#line 127
    source_0[3] = source_0[3] * (RU_Linear(RU_Material_0._Color).w * i_color_0.w) * sourceBounds_0;

#line 127
    vec4 pixelBox_0 = GetPixelBox_0();

#line 127
    vec2 boxUv_2 = (i_effectUv_0 - pixelBox_0.xy) / pixelBox_0.zw + 0.5;

#line 127
    float flowNoise_2 = GetFlowNoise_0(boxUv_2);

#line 127
    float upperFace_0 = GetUpperFaceMask_0(boxUv_2, flowNoise_2);

#line 127
    vec3 faceColor_0 = mix(source_0.xyz, RU_Linear(RU_Material_0._FogColor).xyz, vec3(saturate_0(upperFace_0 * mix(0.81999999284744263, 1.0, GetErosionMask_0(flowNoise_2, upperFace_0)) * saturate_0(RU_Material_0._FogDensity) * RU_Linear(RU_Material_0._FogColor).w)));

#line 127
    clip_0(source_0.w - 0.00100000004749745);

#line 127
    entryPointParam_frag_0 = vec4(faceColor_0, source_0.w);

#line 127
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
