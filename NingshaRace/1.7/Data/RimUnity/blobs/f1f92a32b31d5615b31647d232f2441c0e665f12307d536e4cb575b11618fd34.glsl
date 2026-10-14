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
    float _Brightness; // @default = 1
    float _CanvasPadding; // @default = 0.45
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DetailPreserve; // @default = 0.72
    float _GatherRadius; // @default = 0.85
    float _GrainAmount; // @default = 0.8
    float _GrainContrast; // @default = 2.4
    float _GrainScale; // @default = 96
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    float _OriginalTint; // @default = 0.08
    float _ParticleDensity; // @default = 0.82
    float _ParticleJitter; // @default = 0.35
    float _ParticleScale; // @default = 220
    float _ParticleSize; // @default = 0.42
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 0.42
    vec4 _SandDark; // @default = vec4(0.42, 0.28, 0.13, 1) @color
    vec4 _SandLight; // @default = vec4(1, 0.86, 0.52, 1) @color
    vec4 _SandMid; // @default = vec4(0.74, 0.55, 0.3, 1) @color
    float _SandProgress; // @default = 0.5
    float _ScatterRadius; // @default = 0.9
    float _ShadowInfluence; // @default = 0.75
    float _SolidHold; // @default = 0.035
    float _SparkleAmount; // @default = 0.22
};


#line 84
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AlphaCutoff; // @default = 0.08
    float _Brightness; // @default = 1
    float _CanvasPadding; // @default = 0.45
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DetailPreserve; // @default = 0.72
    float _GatherRadius; // @default = 0.85
    float _GrainAmount; // @default = 0.8
    float _GrainContrast; // @default = 2.4
    float _GrainScale; // @default = 96
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    float _OriginalTint; // @default = 0.08
    float _ParticleDensity; // @default = 0.82
    float _ParticleJitter; // @default = 0.35
    float _ParticleScale; // @default = 220
    float _ParticleSize; // @default = 0.42
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 0.42
    vec4 _SandDark; // @default = vec4(0.42, 0.28, 0.13, 1) @color
    vec4 _SandLight; // @default = vec4(1, 0.86, 0.52, 1) @color
    vec4 _SandMid; // @default = vec4(0.74, 0.55, 0.3, 1) @color
    float _SandProgress; // @default = 0.5
    float _ScatterRadius; // @default = 0.9
    float _ShadowInfluence; // @default = 0.75
    float _SolidHold; // @default = 0.035
    float _SparkleAmount; // @default = 0.22
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


#line 14961
vec2 saturate_0(vec2 x_1)
{

#line 14969
    return clamp(x_1, vec2(0.0), vec2(1.0));
}


#line 14946
float saturate_1(float x_2)
{

#line 14954
    return clamp(x_2, 0.0, 1.0);
}


#line 124 0
vec2 GetStageFactors_0()
{

#line 124
    float progress_0 = saturate_1(RU_Material_0._SandProgress);

#line 124
    return vec2(smoothstep(0.10000000149011612, 0.5, progress_0), smoothstep(0.50999999046325684, 1.0, progress_0));
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
vec2 GetParticleDirection_0(vec2 uv_0)
{

#line 124
    float angle_0 = Hash21_0(floor(uv_0 * max(RU_Material_0._ParticleScale, 1.0)) + vec2(19.17000007629394531, 73.410003662109375)) * 6.28318548202514648;

#line 124
    return vec2(cos(angle_0), sin(angle_0));
}


#line 124
vec2 GetCloudOffset_0(vec2 uv_1, vec2 stage_0)
{

#line 124
    float gather_0 = stage_0.x;

#line 124
    float scatter_0 = stage_0.y;

#line 124
    vec2 direction_0 = GetParticleDirection_0(uv_1);

#line 124
    vec2 _S3;

#line 124
    if((saturate_1(RU_Material_0._SandProgress)) < 0.50999999046325684)
    {

#line 124
        _S3 = direction_0 * RU_Material_0._GatherRadius * (1.0 - gather_0);

#line 124
    }
    else
    {

#line 124
        _S3 = direction_0 * RU_Material_0._ScatterRadius * scatter_0;

#line 124
    }

#line 124
    return _S3;
}


#line 124
float GetUvBoundsMask_0(vec2 uv_2)
{

#line 124
    float _S4 = uv_2.x;

#line 124
    float _S5 = uv_2.y;

#line 124
    return step(0.0, _S4) * step(_S4, 1.0) * step(0.0, _S5) * step(_S5, 1.0);
}


#line 124
float GetSoftUvBoundsMask_0(vec2 uv_3)
{

#line 124
    vec2 edgeDistance_0 = min(uv_3, 1.0 - uv_3);

#line 124
    return smoothstep(-0.05999999865889549, 0.02500000037252903, min(edgeDistance_0.x, edgeDistance_0.y));
}


#line 116
vec4 RU_tex2D_MainTex_0(vec2 uv_4)
{

#line 116
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_4.x, 1.0 - uv_4.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 124
float GetParticleMask_0(vec2 uv_5, float seed_0)
{

#line 124
    vec2 gridUv_0 = uv_5 * max(RU_Material_0._ParticleScale, 1.0);

#line 124
    vec2 cell_0 = floor(gridUv_0);

#line 124
    vec2 local_0 = fract(gridUv_0);

#line 124
    vec2 center_0 = mix(vec2(0.5, 0.5), vec2(Hash21_0(cell_0 + vec2(seed_0, seed_0 * 1.37000000476837158)), Hash21_0(cell_0 + vec2(seed_0 * 2.1099998950958252, seed_0 * 0.73000001907348633))), vec2(saturate_1(RU_Material_0._ParticleJitter)));

#line 124
    float halfSize_0 = RU_Material_0._ParticleSize * mix(0.75, 1.10000002384185791, Hash21_0(cell_0 + vec2(seed_0 * 3.17000007629394531, seed_0 * 4.19000005722045898))) * 0.5;

#line 124
    return (1.0 - smoothstep(halfSize_0, min(halfSize_0 + 0.02500000037252903, 0.5), max(abs(local_0.x - center_0.x), abs(local_0.y - center_0.y)))) * step(1.0 - RU_Material_0._ParticleDensity, Hash21_0(cell_0 + vec2(seed_0 * 5.23000001907348633, seed_0 * 6.28999996185302734)));
}


#line 124
float GetLayeredParticleMask_0(vec2 uv_6, vec2 stage_1)
{

#line 124
    return GetParticleMask_0(uv_6, 11.30000019073486328);
}


#line 124
float GetCanvasFeather_0(vec2 meshUv_0)
{

#line 124
    vec2 edgeDistance_1 = min(meshUv_0, 1.0 - meshUv_0);

#line 124
    return smoothstep(0.0, mix(0.03999999910593033, 0.18000000715255737, saturate_1(abs(RU_Material_0._SandProgress - 0.5) * 2.0)), min(edgeDistance_1.x, edgeDistance_1.y));
}


#line 124
float GetSourceShapeFeather_0(vec2 uv_7)
{

#line 124
    vec2 texel_0 = max(abs(RU_Material_0._MainTex_TexelSize.xy), vec2(0.00000999999974738, 0.00000999999974738));

#line 124
    vec2 _S6 = vec2(texel_0.x, 0.0);

#line 124
    vec2 _S7 = vec2(0.0, texel_0.y);

#line 124
    return smoothstep(0.00999999977648258, 0.2199999988079071, max(RU_tex2D_MainTex_0(uv_7).w, max(max(RU_tex2D_MainTex_0(uv_7 - _S6).w, RU_tex2D_MainTex_0(uv_7 + _S6).w), max(RU_tex2D_MainTex_0(uv_7 - _S7).w, RU_tex2D_MainTex_0(uv_7 + _S7).w))));
}


#line 124
float ValueNoise_0(vec2 uv_8)
{

#line 124
    vec2 cell_1 = floor(uv_8);

#line 124
    vec2 local_1 = fract(uv_8);

#line 124
    vec2 blend_0 = local_1 * local_1 * (3.0 - 2.0 * local_1);

#line 124
    float _S8 = blend_0.x;

#line 124
    return mix(mix(Hash21_0(cell_1), Hash21_0(cell_1 + vec2(1.0, 0.0)), _S8), mix(Hash21_0(cell_1 + vec2(0.0, 1.0)), Hash21_0(cell_1 + vec2(1.0, 1.0)), _S8), blend_0.y);
}


#line 124
float GetGrain_0(vec2 uv_9)
{

#line 124
    return pow(saturate_1(ValueNoise_0(floor(uv_9 * RU_Material_0._GrainScale) / max(RU_Material_0._GrainScale, 1.0) * RU_Material_0._GrainScale + vec2(7.09999990463256836, 19.29999923706054688))), max(RU_Material_0._GrainContrast, 0.00100000004749745));
}


#line 124
float GetLuminance_0(vec3 color_0)
{

#line 124
    return dot(color_0, vec3(0.29899999499320984, 0.58700001239776611, 0.11400000005960464));
}


#line 124
vec3 GetSandColor_0(float luminance_0, float grain_0)
{

#line 124
    float tone_0 = saturate_1(mix(0.41999998688697815, luminance_0, saturate_1(RU_Material_0._ShadowInfluence)) * 0.81999999284744263 + grain_0 * 0.18000000715255737);

#line 124
    return mix(mix(RU_Linear(RU_Material_0._SandDark).xyz, RU_Linear(RU_Material_0._SandMid).xyz, vec3(smoothstep(0.05000000074505806, 0.68000000715255737, tone_0))), RU_Linear(RU_Material_0._SandLight).xyz, vec3(smoothstep(0.57999998331069946, 1.0, tone_0)));
}


#line 124
float GetSourceDetail_0(vec2 uv_10, float luminance_1)
{

#line 124
    vec2 texel_1 = max(abs(RU_Material_0._MainTex_TexelSize.xy), vec2(0.00000999999974738, 0.00000999999974738));

#line 124
    vec2 _S9 = vec2(texel_1.x, 0.0);

#line 124
    vec2 _S10 = vec2(0.0, texel_1.y);

#line 124
    return (luminance_1 - (GetLuminance_0(RU_tex2D_MainTex_0(uv_10 - _S9).xyz) + GetLuminance_0(RU_tex2D_MainTex_0(uv_10 + _S9).xyz) + GetLuminance_0(RU_tex2D_MainTex_0(uv_10 - _S10).xyz) + GetLuminance_0(RU_tex2D_MainTex_0(uv_10 + _S10).xyz)) * 0.25) * 2.0;
}


#line 124
float GetReliefShade_0(vec2 uv_11)
{

#line 124
    vec2 texel_2 = max(abs(RU_Material_0._MainTex_TexelSize.xy), vec2(0.00000999999974738, 0.00000999999974738));

#line 124
    vec2 _S11 = vec2(texel_2.x, 0.0);

#line 124
    vec2 _S12 = vec2(0.0, texel_2.y);

#line 124
    return mix(1.0, mix(0.81999999284744263, 1.12000000476837158, dot(normalize(vec3(- vec2(GetLuminance_0(RU_tex2D_MainTex_0(uv_11 + _S11).xyz) - GetLuminance_0(RU_tex2D_MainTex_0(uv_11 - _S11).xyz), GetLuminance_0(RU_tex2D_MainTex_0(uv_11 + _S12).xyz) - GetLuminance_0(RU_tex2D_MainTex_0(uv_11 - _S12).xyz)) * 3.0, 1.0)), normalize(vec3(RU_Material_0._ReliefLightDirection.xy, 1.0))) * 0.5 + 0.5), saturate_1(RU_Material_0._ReliefStrength));
}


#line 124
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 124
layout(location = 5) in vec2 i_meshUv_0; // @vertex TexCoord1 optional


#line 124
layout(location = 6) in vec2 i_rawUv_0; // @vertex TexCoord2 optional


#line 124
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 124
void RU_SourceMain()
{

#line 124
    vec2 stage_2 = GetStageFactors_0();

#line 124
    float progress_1 = saturate_1(RU_Material_0._SandProgress);

#line 124
    vec2 targetUv_0 = i_rawUv_0 - GetCloudOffset_0(i_rawUv_0, stage_2);

#line 124
    vec2 sampledTargetUv_0 = saturate_0(targetUv_0);

#line 124
    vec4 source_0 = RU_tex2D_MainTex_0(sampledTargetUv_0) * RU_Linear(RU_Material_0._Color) * i_color_0;

#line 124
    float alpha_0 = source_0.w * (GetUvBoundsMask_0(targetUv_0) * GetSoftUvBoundsMask_0(targetUv_0));

#line 124
    float particleMask_0 = GetLayeredParticleMask_0(i_rawUv_0, stage_2);

#line 124
    bool _S13 = progress_1 < 0.50999999046325684;

#line 124
    float bodyPhase_0;

#line 124
    if(_S13)
    {

#line 124
        bodyPhase_0 = smoothstep(0.07999999821186066, 0.2800000011920929, stage_2.x);

#line 124
    }
    else
    {

#line 124
        bodyPhase_0 = 1.0 - smoothstep(0.72000002861022949, 1.0, stage_2.y);

#line 124
    }

#line 124
    float startFade_0 = smoothstep(0.01999999955296516, 0.10000000149011612, progress_1);

#line 124
    float endFade_0;

#line 124
    if(_S13)
    {

#line 124
        endFade_0 = 1.0;

#line 124
    }
    else
    {

#line 124
        endFade_0 = 1.0 - smoothstep(0.86000001430511475, 1.0, stage_2.y);

#line 124
    }

#line 124
    float solidHold_0 = 1.0 - smoothstep(RU_Material_0._SolidHold, RU_Material_0._SolidHold * 2.0, abs(progress_1 - 0.5));

#line 124
    float bodyAlpha_0 = alpha_0 * mix(particleMask_0, 1.0, solidHold_0) * bodyPhase_0 * startFade_0 * endFade_0 * GetCanvasFeather_0(i_meshUv_0) * mix(GetSourceShapeFeather_0(sampledTargetUv_0), 1.0, solidHold_0);

#line 124
    float visibleMask_0 = saturate_1(bodyAlpha_0);

#line 124
    clip_0(visibleMask_0 - RU_Material_0._AlphaCutoff * 0.34999999403953552);

#line 124
    float grain_1 = GetGrain_0(i_rawUv_0);

#line 124
    float sourceInfluence_0 = saturate_1(bodyAlpha_0 * 2.0);

#line 124
    vec3 _S14 = source_0.xyz;

#line 124
    float luminance_2 = mix(0.41999998688697815, GetLuminance_0(_S14), sourceInfluence_0);

#line 124
    vec3 finalRgb_0 = GetSandColor_0(luminance_2, grain_1) * mix(1.0, mix(0.77999997138977051, 1.10000002384185791, grain_1), RU_Material_0._GrainAmount) * mix(1.0, GetReliefShade_0(sampledTargetUv_0), sourceInfluence_0) * clamp(1.0 + GetSourceDetail_0(sampledTargetUv_0, luminance_2) * sourceInfluence_0 * RU_Material_0._DetailPreserve, 0.72000002861022949, 1.08000004291534424);

#line 124
    entryPointParam_frag_0 = vec4(mix(mix(finalRgb_0, finalRgb_0 * _S14, vec3(saturate_1(RU_Material_0._OriginalTint) * sourceInfluence_0)), RU_Linear(RU_Material_0._SandLight).xyz, vec3(saturate_1(smoothstep(0.89999997615814209, 1.0, grain_1) * RU_Material_0._SparkleAmount * 0.44999998807907104))) * RU_Material_0._Brightness, visibleMask_0 * mix(1.0, saturate_1(0.72000002861022949 + grain_1 * 0.44999998807907104), RU_Material_0._GrainAmount * 0.44999998807907104));

#line 124
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
