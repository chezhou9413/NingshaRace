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
    float _Alpha; // @default = 0.95
    float _ArcEnd; // @default = 2.85
    float _ArcStart; // @default = -0.18
    vec4 _CrackColor; // @default = vec4(1, 0.83, 0.38, 1) @color
    float _CrackScale; // @default = 28
    float _CrackStrength; // @default = 0.75
    float _CrackWidth; // @default = 0.045
    float _DepthFade; // @default = 0.8
    vec4 _EarthDark; // @default = vec4(0.16, 0.075, 0.025, 1) @color
    vec4 _EarthLight; // @default = vec4(0.95, 0.58, 0.16, 1) @color
    vec4 _EarthMid; // @default = vec4(0.52, 0.24, 0.055, 1) @color
    float _EdgeSoftness; // @default = 0.035
    float _Glow; // @default = 1.4
    vec4 _InnerOffset; // @default = vec4(0.18, -0.1, 0, 0)
    float _InnerRadius; // @default = 0.65
    float _MistAlpha; // @default = 0.28
    vec4 _MistColor; // @default = vec4(0.72, 0.46, 0.18, 1) @color
    float _MistIrregularity; // @default = 0.75
    float _MistScale; // @default = 10
    float _MistSoftness; // @default = 0.12
    float _MistSpread; // @default = 0.16
    float _MistStretch; // @default = 5
    float _MotionSpeed; // @default = 0.35
    float _OuterRadius; // @default = 0.92
    float _RockContrast; // @default = 2.2
    float _RockScale; // @default = 15
    float _Sweep; // @default = 0.16
    vec4 _Time;
    float _TipTaper; // @default = 0.72
    vec4 _WindColor; // @default = vec4(0.86, 0.72, 0.46, 1) @color
    float _WindScale; // @default = 12
    float _WindSharpness; // @default = 3.2
    float _WindSpeed; // @default = 1.2
    float _WindStrength; // @default = 0.65
    float _WindStretch; // @default = 9
};


#line 93
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _Alpha; // @default = 0.95
    float _ArcEnd; // @default = 2.85
    float _ArcStart; // @default = -0.18
    vec4 _CrackColor; // @default = vec4(1, 0.83, 0.38, 1) @color
    float _CrackScale; // @default = 28
    float _CrackStrength; // @default = 0.75
    float _CrackWidth; // @default = 0.045
    float _DepthFade; // @default = 0.8
    vec4 _EarthDark; // @default = vec4(0.16, 0.075, 0.025, 1) @color
    vec4 _EarthLight; // @default = vec4(0.95, 0.58, 0.16, 1) @color
    vec4 _EarthMid; // @default = vec4(0.52, 0.24, 0.055, 1) @color
    float _EdgeSoftness; // @default = 0.035
    float _Glow; // @default = 1.4
    vec4 _InnerOffset; // @default = vec4(0.18, -0.1, 0, 0)
    float _InnerRadius; // @default = 0.65
    float _MistAlpha; // @default = 0.28
    vec4 _MistColor; // @default = vec4(0.72, 0.46, 0.18, 1) @color
    float _MistIrregularity; // @default = 0.75
    float _MistScale; // @default = 10
    float _MistSoftness; // @default = 0.12
    float _MistSpread; // @default = 0.16
    float _MistStretch; // @default = 5
    float _MotionSpeed; // @default = 0.35
    float _OuterRadius; // @default = 0.92
    float _RockContrast; // @default = 2.2
    float _RockScale; // @default = 15
    float _Sweep; // @default = 0.16
    vec4 _Time;
    float _TipTaper; // @default = 0.72
    vec4 _WindColor; // @default = vec4(0.86, 0.72, 0.46, 1) @color
    float _WindScale; // @default = 12
    float _WindSharpness; // @default = 3.2
    float _WindSpeed; // @default = 1.2
    float _WindStrength; // @default = 0.65
    float _WindStretch; // @default = 9
}RU_Material_0;

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


#line 131 0
float GetCrescentMask_0(vec2 uv_0)
{

#line 131
    vec2 _S1 = (uv_0 - 0.5) * 2.0;

#line 131
    vec2 p_0 = _S1;

#line 131
    float _S2 = _S1.y;

#line 131
    p_0[0] = p_0[0] + RU_Material_0._Sweep * (_S2 * _S2 - 0.25);

#line 131
    float arcT_0 = saturate_0(((atan((p_0.y),(p_0.x))) - RU_Material_0._ArcStart) / max(RU_Material_0._ArcEnd - RU_Material_0._ArcStart, 0.00999999977648258));

#line 131
    return saturate_0((1.0 - smoothstep(RU_Material_0._OuterRadius, RU_Material_0._OuterRadius + RU_Material_0._EdgeSoftness, length(p_0))) * smoothstep(RU_Material_0._InnerRadius - RU_Material_0._EdgeSoftness, RU_Material_0._InnerRadius + RU_Material_0._EdgeSoftness, length(p_0 - RU_Material_0._InnerOffset.xy)) * (smoothstep(0.0, 0.10000000149011612, arcT_0) * (1.0 - smoothstep(0.89999997615814209, 1.0, arcT_0))) * mix(1.0, smoothstep(0.0, 0.5, arcT_0) * (1.0 - smoothstep(0.5, 1.0, arcT_0)) * 2.0, RU_Material_0._TipTaper));
}


#line 131
float Hash21_0(vec2 value_0)
{

#line 131
    vec2 _S3 = fract(value_0 * vec2(123.339996337890625, 456.209991455078125));

#line 131
    vec2 _S4 = _S3 + dot(_S3, _S3 + 45.31999969482421875);

#line 131
    return fract(_S4.x * _S4.y);
}


#line 131
float ValueNoise_0(vec2 uv_1)
{

#line 131
    vec2 cell_0 = floor(uv_1);

#line 131
    vec2 local_0 = fract(uv_1);

#line 131
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 131
    float _S5 = blend_0.x;

#line 131
    return mix(mix(Hash21_0(cell_0), Hash21_0(cell_0 + vec2(1.0, 0.0)), _S5), mix(Hash21_0(cell_0 + vec2(0.0, 1.0)), Hash21_0(cell_0 + vec2(1.0, 1.0)), _S5), blend_0.y);
}


#line 131
float GetRockPattern_0(vec2 uv_2)
{

#line 131
    vec2 flowUv_0 = uv_2 * RU_Material_0._RockScale + vec2(RU_Material_0._Time.y * RU_Material_0._MotionSpeed, 0.0);

#line 131
    return pow(saturate_0(mix(ValueNoise_0(flowUv_0), ValueNoise_0(flowUv_0 * 2.70000004768371582 + 17.39999961853027344), 0.37999999523162842)), max(RU_Material_0._RockContrast, 0.00999999977648258));
}


#line 131
float GetCrackMask_0(vec2 uv_3)
{

#line 131
    vec2 crackUv_0 = uv_3 * RU_Material_0._CrackScale + vec2(0.0, RU_Material_0._Time.y * RU_Material_0._MotionSpeed * 0.60000002384185791);

#line 131
    return 1.0 - smoothstep(RU_Material_0._CrackWidth, RU_Material_0._CrackWidth + 0.03500000014901161, abs(ValueNoise_0(crackUv_0) - ValueNoise_0(crackUv_0 * 1.83000004291534424 + 31.70000076293945312)));
}


#line 131
vec2 GetCrescentFlowCoordinates_0(vec2 uv_4)
{

#line 131
    vec2 _S6 = (uv_4 - 0.5) * 2.0;

#line 131
    vec2 p_1 = _S6;

#line 131
    float _S7 = _S6.y;

#line 131
    p_1[0] = p_1[0] + RU_Material_0._Sweep * (_S7 * _S7 - 0.25);

#line 131
    return vec2(saturate_0(((atan((p_1.y),(p_1.x))) - RU_Material_0._ArcStart) / max(RU_Material_0._ArcEnd - RU_Material_0._ArcStart, 0.00999999977648258)), length(p_1) / max(RU_Material_0._OuterRadius, 0.00100000004749745));
}


#line 131
float GetArcWind_0(vec2 uv_5)
{

#line 131
    vec2 crescentUv_0 = GetCrescentFlowCoordinates_0(uv_5);

#line 131
    float _S8 = crescentUv_0.x;

#line 131
    float along_0 = _S8 * RU_Material_0._WindScale - RU_Material_0._Time.y * RU_Material_0._WindSpeed;

#line 131
    float across_0 = crescentUv_0.y * RU_Material_0._WindScale * RU_Material_0._WindStretch;

#line 131
    return pow(saturate_0(ValueNoise_0(vec2(along_0, across_0)) * 0.68000000715255737 + ValueNoise_0(vec2(along_0 * 1.70000004768371582 - RU_Material_0._Time.y * RU_Material_0._WindSpeed * 0.44999998807907104, across_0 * 1.85000002384185791 + 19.29999923706054688)) * 0.2199999988079071 + ValueNoise_0(vec2(along_0 * 0.41999998688697815 + 37.09999847412109375, across_0 * 0.2199999988079071)) * 0.10000000149011612), max(RU_Material_0._WindSharpness, 0.00999999977648258)) * smoothstep(0.02999999932944775, 0.18000000715255737, _S8) * (1.0 - smoothstep(0.81999999284744263, 0.98000001907348633, _S8));
}


#line 131
float GetOuterMist_0(vec2 uv_6)
{

#line 131
    vec2 _S9 = (uv_6 - 0.5) * 2.0;

#line 131
    vec2 p_2 = _S9;

#line 131
    float _S10 = _S9.y;

#line 131
    p_2[0] = p_2[0] + RU_Material_0._Sweep * (_S10 * _S10 - 0.25);

#line 131
    float radius_0 = length(p_2);

#line 131
    float arcT_1 = saturate_0(((atan((p_2.y),(p_2.x))) - RU_Material_0._ArcStart) / max(RU_Material_0._ArcEnd - RU_Material_0._ArcStart, 0.00999999977648258));

#line 131
    float along_1 = arcT_1 * RU_Material_0._MistScale - RU_Material_0._Time.y * RU_Material_0._WindSpeed;

#line 131
    float across_1 = (radius_0 - RU_Material_0._OuterRadius) * RU_Material_0._MistScale * RU_Material_0._MistStretch;

#line 131
    float distortedRadius_0 = radius_0 - (ValueNoise_0(vec2(along_1 * 0.47999998927116394, across_1 * 0.34999999403953552 + 13.69999980926513672)) - 0.5) * RU_Material_0._MistIrregularity * RU_Material_0._MistSpread;

#line 131
    return smoothstep(RU_Material_0._OuterRadius - RU_Material_0._MistSoftness, RU_Material_0._OuterRadius + RU_Material_0._MistSoftness, distortedRadius_0) * (1.0 - smoothstep(RU_Material_0._OuterRadius, RU_Material_0._OuterRadius + RU_Material_0._MistSpread, distortedRadius_0)) * (smoothstep(0.0, 0.11999999731779099, arcT_1) * (1.0 - smoothstep(0.8399999737739563, 1.0, arcT_1))) * mix(1.0, smoothstep(0.0, 0.5, arcT_1) * (1.0 - smoothstep(0.5, 1.0, arcT_1)) * 2.0, RU_Material_0._TipTaper) * smoothstep(0.2800000011920929, 0.77999997138977051, ValueNoise_0(vec2(along_1 * 0.41999998688697815, across_1 * 0.55000001192092896)) * 0.51999998092651367 + ValueNoise_0(vec2(along_1 * 1.29999995231628418 - RU_Material_0._Time.y * RU_Material_0._WindSpeed * 0.34999999403953552, across_1 * 1.60000002384185791 + 27.10000038146972656)) * 0.2800000011920929 + ValueNoise_0(uv_6 * RU_Material_0._MistScale * 0.75 + vec2(- RU_Material_0._Time.y * RU_Material_0._WindSpeed * 0.18000000715255737, 41.20000076293945312)) * 0.20000000298023224);
}


#line 131
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 131
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 131
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 131
void RU_SourceMain()
{

#line 131
    float mask_0 = GetCrescentMask_0(i_uv_0);

#line 131
    float rock_0 = GetRockPattern_0(i_uv_0);

#line 131
    float crack_0 = GetCrackMask_0(i_uv_0) * RU_Material_0._CrackStrength;

#line 131
    float wind_0 = GetArcWind_0(i_uv_0) * RU_Material_0._WindStrength;

#line 131
    float mistAlpha_0 = GetOuterMist_0(i_uv_0) * RU_Material_0._MistAlpha * i_color_0.w;

#line 131
    vec3 finalColor_0 = mix(mix(mix(mix(mix(RU_Linear(RU_Material_0._EarthDark).xyz, RU_Linear(RU_Material_0._EarthMid).xyz, vec3(smoothstep(0.15000000596046448, 0.62000000476837158, rock_0))), RU_Linear(RU_Material_0._EarthLight).xyz, vec3(smoothstep(0.60000002384185791, 0.95999997854232788, rock_0))) * (0.81999999284744263 + saturate_0(1.0 - smoothstep(0.18000000715255737, 0.75, abs(length((i_uv_0 - 0.5) * 2.0) - RU_Material_0._OuterRadius) * 3.20000004768371582)) * RU_Material_0._Glow * 0.25), RU_Linear(RU_Material_0._CrackColor).xyz, vec3(saturate_0(crack_0))) + RU_Linear(RU_Material_0._CrackColor).xyz * crack_0 * RU_Material_0._Glow * 0.34999999403953552, RU_Linear(RU_Material_0._WindColor).xyz, vec3(saturate_0(wind_0))) + RU_Linear(RU_Material_0._WindColor).xyz * wind_0 * (0.18000000715255737 + RU_Material_0._Glow * 0.07999999821186066), RU_Linear(RU_Material_0._MistColor).xyz, vec3(saturate_0(mistAlpha_0 * 1.35000002384185791))) + RU_Linear(RU_Material_0._MistColor).xyz * mistAlpha_0 * (0.15999999642372131 + RU_Material_0._Glow * 0.03999999910593033);

#line 131
    float alpha_0 = saturate_0(mask_0 * pow(saturate_0(mask_0), max(RU_Material_0._DepthFade, 0.00999999977648258)) * RU_Material_0._Alpha * i_color_0.w + mistAlpha_0);

#line 131
    clip_0(alpha_0 - 0.0020000000949949);

#line 131
    entryPointParam_frag_0 = vec4(finalColor_0, alpha_0);

#line 131
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
