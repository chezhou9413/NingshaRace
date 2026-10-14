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


#line 56 0
struct RU_MaterialData_0
{
    float _AlphaCutoff; // @default = 0.08
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _CrackAmount; // @default = 0.34
    float _CrackScale; // @default = 28
    float _CrackWidth; // @default = 0.022
    float _DetailPreserve; // @default = 0.82
    float _DissolveProgress; // @default = 0
    float _DissolveSoftness; // @default = 0.085
    vec4 _EdgeColor; // @default = vec4(0.58, 0.57, 0.51, 1) @color
    float _EdgeWidth; // @default = 0.035
    float _GrainAmount; // @default = 0.32
    float _GrainScale; // @default = 34
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    float _NoiseContrast; // @default = 1.45
    float _NoiseScale; // @default = 18.2
    float _OriginalTint; // @default = 0.03
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 0.62
    float _RevealProgress; // @default = 1
    float _ShadowInfluence; // @default = 0.86
    vec4 _StoneDark; // @default = vec4(0.16, 0.17, 0.17, 1) @color
    vec4 _StoneLight; // @default = vec4(0.69, 0.68, 0.62, 1) @color
    vec4 _StoneMid; // @default = vec4(0.43, 0.44, 0.42, 1) @color
    float _StoneRoughness; // @default = 0.58
    float _VerticalBias; // @default = 0.4
};


#line 84
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AlphaCutoff; // @default = 0.08
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _CrackAmount; // @default = 0.34
    float _CrackScale; // @default = 28
    float _CrackWidth; // @default = 0.022
    float _DetailPreserve; // @default = 0.82
    float _DissolveProgress; // @default = 0
    float _DissolveSoftness; // @default = 0.085
    vec4 _EdgeColor; // @default = vec4(0.58, 0.57, 0.51, 1) @color
    float _EdgeWidth; // @default = 0.035
    float _GrainAmount; // @default = 0.32
    float _GrainScale; // @default = 34
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    float _NoiseContrast; // @default = 1.45
    float _NoiseScale; // @default = 18.2
    float _OriginalTint; // @default = 0.03
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 0.62
    float _RevealProgress; // @default = 1
    float _ShadowInfluence; // @default = 0.86
    vec4 _StoneDark; // @default = vec4(0.16, 0.17, 0.17, 1) @color
    vec4 _StoneLight; // @default = vec4(0.69, 0.68, 0.62, 1) @color
    vec4 _StoneMid; // @default = vec4(0.43, 0.44, 0.42, 1) @color
    float _StoneRoughness; // @default = 0.58
    float _VerticalBias; // @default = 0.4
    float _RimUnitySrgb_MainTex; // @default = 1
}RU_Material_0;

#line 112
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 114



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


#line 116 0
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 116
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 124
float Hash21_0(vec2 value_0)
{

#line 124
    vec2 _S1 = fract(value_0 * vec2(123.339996337890625, 456.209991455078125));

#line 124
    vec2 _S2 = _S1 + dot(_S1, _S1 + 45.31999969482421875);

#line 124
    return fract(_S2.x * _S2.y);
}


#line 124
float ValueNoise_0(vec2 uv_1)
{

#line 124
    vec2 cell_0 = floor(uv_1);

#line 124
    vec2 local_0 = fract(uv_1);

#line 124
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 124
    float _S3 = blend_0.x;

#line 124
    return mix(mix(Hash21_0(cell_0), Hash21_0(cell_0 + vec2(1.0, 0.0)), _S3), mix(Hash21_0(cell_0 + vec2(0.0, 1.0)), Hash21_0(cell_0 + vec2(1.0, 1.0)), _S3), blend_0.y);
}


#line 124
float FractalNoise_0(vec2 uv_2)
{

#line 124
    return saturate_0(ValueNoise_0(uv_2) * 0.57999998331069946 + ValueNoise_0(uv_2 * 2.02999997138977051 + 17.70000076293945312) * 0.2800000011920929 + ValueNoise_0(uv_2 * 4.1100001335144043 + 43.20000076293945312) * 0.14000000059604645);
}


#line 124
vec2 GetVisibility_0(vec2 uv_3)
{

#line 124
    float baseNoise_0 = FractalNoise_0(uv_3 * RU_Material_0._NoiseScale + vec2(13.5, 41.20000076293945312));

#line 124
    float biasedNoise_0 = pow(saturate_0(mix(baseNoise_0, saturate_0(baseNoise_0 * 0.72000002861022949 + uv_3.y * 0.56000000238418579), saturate_0(RU_Material_0._VerticalBias))), max(RU_Material_0._NoiseContrast, 0.00100000004749745));

#line 124
    float _S4 = max(RU_Material_0._DissolveSoftness, 0.00100000004749745);

#line 124
    float revealThreshold_0 = 1.0 - saturate_0(RU_Material_0._RevealProgress);

#line 124
    float dissolveThreshold_0 = 1.0 - saturate_0(RU_Material_0._DissolveProgress);

#line 124
    float reveal_0;

#line 124
    if((saturate_0(RU_Material_0._RevealProgress)) >= 0.99900001287460327)
    {

#line 124
        reveal_0 = 1.0;

#line 124
    }
    else
    {

#line 124
        reveal_0 = smoothstep(revealThreshold_0 - _S4, revealThreshold_0 + _S4, biasedNoise_0);

#line 124
    }

#line 124
    float remain_0;

#line 124
    if((saturate_0(RU_Material_0._DissolveProgress)) <= 0.00100000004749745)
    {

#line 124
        remain_0 = 1.0;

#line 124
    }
    else
    {

#line 124
        remain_0 = 1.0 - smoothstep(dissolveThreshold_0 - _S4, dissolveThreshold_0 + _S4, biasedNoise_0);

#line 124
    }

#line 124
    return vec2(reveal_0 * remain_0, saturate_0(max((1.0 - smoothstep(RU_Material_0._EdgeWidth, RU_Material_0._EdgeWidth + _S4, abs(biasedNoise_0 - revealThreshold_0))) * step(RU_Material_0._RevealProgress, 0.99900001287460327), (1.0 - smoothstep(RU_Material_0._EdgeWidth, RU_Material_0._EdgeWidth + _S4, abs(biasedNoise_0 - dissolveThreshold_0))) * step(0.00100000004749745, RU_Material_0._DissolveProgress))));
}


#line 124
float GetLuminance_0(vec3 color_0)
{

#line 124
    return dot(color_0, vec3(0.29899999499320984, 0.58700001239776611, 0.11400000005960464));
}


#line 124
float GetNeighborLuminance_0(vec2 uv_4, vec4 tint_0, float centerLuminance_0)
{

#line 124
    vec4 sample_0 = RU_tex2D_MainTex_0(uv_4) * tint_0;

#line 124
    return mix(centerLuminance_0, GetLuminance_0(sample_0.xyz), saturate_0(sample_0.w));
}


#line 124
float GetStoneGrain_0(vec2 uv_5)
{

#line 124
    vec2 _S5 = uv_5 * max(RU_Material_0._GrainScale, 1.0);

#line 124
    vec2 warpedUv_0 = _S5 + (vec2(ValueNoise_0(_S5 * 0.17000000178813934 + vec2(7.09999990463256836, 19.29999923706054688)), ValueNoise_0(_S5 * 0.23000000417232513 + vec2(31.70000076293945312, 5.40000009536743164))) - 0.5) * 2.79999995231628418;

#line 124
    return saturate_0(mix(FractalNoise_0(warpedUv_0 * 0.2199999988079071 + vec2(13.80000019073486328, 2.59999990463256836)), FractalNoise_0(warpedUv_0 + vec2(43.09999847412109375, 17.89999961853027344)), 0.41999998688697815));
}


#line 124
float GetCrackMask_0(vec2 uv_6)
{

#line 124
    float _S6 = max(RU_Material_0._CrackWidth, 0.00100000004749745);

#line 124
    return (1.0 - smoothstep(_S6, _S6 * 2.59999990463256836, abs(FractalNoise_0(uv_6 * RU_Material_0._CrackScale + vec2(31.39999961853027344, 9.80000019073486328)) - 0.5))) * smoothstep(0.41999998688697815, 0.89999997615814209, ValueNoise_0(uv_6 * RU_Material_0._CrackScale * 0.43000000715255737 + 67.1999969482421875)) * saturate_0(RU_Material_0._CrackAmount);
}


#line 124
vec3 GetStoneColor_0(float luminance_0, float stoneNoise_0)
{

#line 124
    float tone_0 = saturate_0(mix(0.54000002145767212, luminance_0, saturate_0(RU_Material_0._ShadowInfluence)) * 0.77999997138977051 + stoneNoise_0 * 0.2800000011920929);

#line 124
    return mix(mix(RU_Linear(RU_Material_0._StoneDark).xyz, RU_Linear(RU_Material_0._StoneMid).xyz, vec3(smoothstep(0.05000000074505806, 0.6600000262260437, tone_0))), RU_Linear(RU_Material_0._StoneLight).xyz, vec3(smoothstep(0.57999998331069946, 1.0, tone_0)));
}


#line 124
float GetSourceDetail_0(float luminance_1, float left_0, float right_0, float down_0, float up_0)
{

#line 124
    return (luminance_1 - (left_0 + right_0 + down_0 + up_0) * 0.25) * 2.0;
}


#line 124
float GetReliefShade_0(float left_1, float right_1, float down_1, float up_1)
{

#line 124
    return mix(1.0, mix(0.68000000715255737, 1.3200000524520874, dot(normalize(vec3(- vec2(right_1 - left_1, up_1 - down_1) * 3.5, 1.0)), normalize(vec3(RU_Material_0._ReliefLightDirection.xy, 1.0))) * 0.5 + 0.5), saturate_0(RU_Material_0._ReliefStrength));
}


#line 124
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 124
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 124
layout(location = 5) in vec2 i_rawUv_0; // @vertex TexCoord1 optional


#line 124
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 124
void RU_SourceMain()
{

#line 124
    vec4 tint_1 = RU_Linear(RU_Material_0._Color) * i_color_0;

#line 124
    vec4 source_0 = RU_tex2D_MainTex_0(i_uv_0) * tint_1;

#line 124
    vec2 visibility_0 = GetVisibility_0(i_rawUv_0);

#line 124
    float visibleMask_0 = saturate_0(source_0.w * visibility_0.x);

#line 124
    clip_0(visibleMask_0 - RU_Material_0._AlphaCutoff);

#line 124
    vec3 _S7 = source_0.xyz;

#line 124
    float luminance_2 = GetLuminance_0(_S7);

#line 124
    vec2 texel_0 = max(abs(RU_Material_0._MainTex_TexelSize.xy), vec2(0.00000999999974738, 0.00000999999974738));

#line 124
    vec2 _S8 = vec2(texel_0.x, 0.0);

#line 124
    float left_2 = GetNeighborLuminance_0(i_uv_0 - _S8, tint_1, luminance_2);

#line 124
    float right_2 = GetNeighborLuminance_0(i_uv_0 + _S8, tint_1, luminance_2);

#line 124
    vec2 _S9 = vec2(0.0, texel_0.y);

#line 124
    float down_2 = GetNeighborLuminance_0(i_uv_0 - _S9, tint_1, luminance_2);

#line 124
    float up_2 = GetNeighborLuminance_0(i_uv_0 + _S9, tint_1, luminance_2);

#line 124
    float stoneNoise_1 = FractalNoise_0(i_rawUv_0 * RU_Material_0._NoiseScale * 0.62000000476837158 + vec2(3.70000004768371582, 28.10000038146972656));

#line 124
    vec3 finalRgb_0 = GetStoneColor_0(luminance_2, stoneNoise_1) * GetReliefShade_0(left_2, right_2, down_2, up_2) * mix(1.0, mix(0.72000002861022949, 1.22000002861022949, GetStoneGrain_0(i_rawUv_0)), saturate_0(RU_Material_0._GrainAmount)) * mix(1.0, mix(0.81999999284744263, 1.15999996662139893, stoneNoise_1), saturate_0(RU_Material_0._StoneRoughness)) * (1.0 + GetSourceDetail_0(luminance_2, left_2, right_2, down_2, up_2) * RU_Material_0._DetailPreserve);

#line 124
    entryPointParam_frag_0 = vec4(mix(mix(mix(finalRgb_0, finalRgb_0 * _S7, vec3(saturate_0(RU_Material_0._OriginalTint))), RU_Linear(RU_Material_0._StoneDark).xyz * 0.62000000476837158, vec3(GetCrackMask_0(i_rawUv_0))), RU_Linear(RU_Material_0._EdgeColor).xyz, vec3(saturate_0(visibility_0.y * visibleMask_0))), visibleMask_0);

#line 124
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
