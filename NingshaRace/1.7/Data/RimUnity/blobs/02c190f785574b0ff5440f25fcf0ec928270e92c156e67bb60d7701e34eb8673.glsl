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
    float _AlphaCutoff; // @default = 0.092
    float _Brightness; // @default = 0.82
    float _CanvasPadding; // @default = 0.95
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DetailPreserve; // @default = 1
    float _GatherRadius; // @default = 0.991
    float _GrainAmount; // @default = 1
    float _GrainContrast; // @default = 0.2
    float _GrainScale; // @default = 256
    vec4 _MainTex_TexelSize;
    float _OriginalTint; // @default = 0.35
    float _ParticleDensity; // @default = 0.72
    float _ParticleJitter; // @default = 0.6
    float _ParticleScale; // @default = 140
    float _ParticleSize; // @default = 0.62
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 1
    vec4 _SandDark; // @default = vec4(0.358, 0.34, 0.323, 1) @color
    vec4 _SandLight; // @default = vec4(0.66, 0.632, 0.564, 1) @color
    vec4 _SandMid; // @default = vec4(0.679, 0.617, 0.548, 1) @color
    float _SandProgress; // @default = 0.5
    float _ScatterRadius; // @default = 1
    float _ShadowInfluence; // @default = 1
    float _SolidHold; // @default = 0.035
    float _SparkleAmount; // @default = 0.104
};


#line 83
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AlphaCutoff; // @default = 0.092
    float _Brightness; // @default = 0.82
    float _CanvasPadding; // @default = 0.95
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DetailPreserve; // @default = 1
    float _GatherRadius; // @default = 0.991
    float _GrainAmount; // @default = 1
    float _GrainContrast; // @default = 0.2
    float _GrainScale; // @default = 256
    vec4 _MainTex_TexelSize;
    float _OriginalTint; // @default = 0.35
    float _ParticleDensity; // @default = 0.72
    float _ParticleJitter; // @default = 0.6
    float _ParticleScale; // @default = 140
    float _ParticleSize; // @default = 0.62
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 1
    vec4 _SandDark; // @default = vec4(0.358, 0.34, 0.323, 1) @color
    vec4 _SandLight; // @default = vec4(0.66, 0.632, 0.564, 1) @color
    vec4 _SandMid; // @default = vec4(0.679, 0.617, 0.548, 1) @color
    float _SandProgress; // @default = 0.5
    float _ScatterRadius; // @default = 1
    float _ShadowInfluence; // @default = 1
    float _SolidHold; // @default = 0.035
    float _SparkleAmount; // @default = 0.104
    float _RimUnitySrgb_MainTex; // @default = 1
}RU_Material_0;

#line 110
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 112



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


#line 114 0
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 114
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 140
vec4 SamplePawn_0(vec2 uv_1)
{

#line 140
    float _S1 = uv_1.x;

#line 140
    float _S2 = uv_1.y;

#line 140
    float bounds_0 = step(0.0, _S1) * step(_S1, 1.0) * step(0.0, _S2) * step(_S2, 1.0);

#line 140
    vec4 sample_0 = RU_tex2D_MainTex_0(saturate_0(uv_1));

#line 140
    sample_0[3] = sample_0[3] * bounds_0;

#line 140
    return sample_0;
}


#line 140
float Hash21_0(vec2 value_0)
{

#line 140
    vec2 _S3 = fract(value_0 * vec2(123.339996337890625, 456.209991455078125));

#line 140
    vec2 _S4 = _S3 + dot(_S3, _S3 + 45.31999969482421875);

#line 140
    return fract(_S4.x * _S4.y);
}


#line 140
float ValueNoise_0(vec2 uv_2)
{

#line 140
    vec2 cell_0 = floor(uv_2);

#line 140
    vec2 local_0 = fract(uv_2);

#line 140
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 140
    float _S5 = blend_0.x;

#line 140
    return mix(mix(Hash21_0(cell_0), Hash21_0(cell_0 + vec2(1.0, 0.0)), _S5), mix(Hash21_0(cell_0 + vec2(0.0, 1.0)), Hash21_0(cell_0 + 1.0), _S5), blend_0.y);
}


#line 140
float GetPeelTime_0(vec2 uv_3)
{

#line 140
    return 0.03500000014901161 + 0.68000000715255737 * (dot(uv_3, vec2(0.43999999761581421, -0.56000000238418579)) + 0.56000000238418579);
}


#line 140
float GetRemainingBody_0(vec2 uv_4, float dissolve_0)
{

#line 140
    float peel_0 = GetPeelTime_0(uv_4) + (ValueNoise_0(uv_4 * 90.0) - 0.5) * 0.01799999922513962;

#line 140
    float _S6 = max((fwidth((peel_0))), 0.0020000000949949);

#line 140
    return 1.0 - smoothstep(peel_0 - _S6, peel_0 + _S6, dissolve_0);
}


#line 140
float GetGrain_0(vec2 uv_5)
{

#line 140
    return pow(saturate_1(ValueNoise_0(floor(uv_5 * RU_Material_0._GrainScale) + vec2(7.09999990463256836, 19.29999923706054688))), max(RU_Material_0._GrainContrast, 0.00100000004749745));
}


#line 140
float GetLuminance_0(vec3 color_0)
{

#line 140
    return dot(color_0, vec3(0.29899999499320984, 0.58700001239776611, 0.11400000005960464));
}


#line 140
vec3 GetSandColor_0(float luminance_0, float grain_0)
{

#line 140
    float tone_0 = saturate_1(mix(0.41999998688697815, luminance_0, saturate_1(RU_Material_0._ShadowInfluence)) * 0.81999999284744263 + grain_0 * 0.18000000715255737);

#line 140
    return mix(mix(RU_Linear(RU_Material_0._SandDark).xyz, RU_Linear(RU_Material_0._SandMid).xyz, vec3(smoothstep(0.05000000074505806, 0.68000000715255737, tone_0))), RU_Linear(RU_Material_0._SandLight).xyz, vec3(smoothstep(0.57999998331069946, 1.0, tone_0)));
}


#line 140
float GetSourceDetail_0(vec2 uv_6, float luminance_1)
{

#line 140
    vec2 texel_0 = max(abs(RU_Material_0._MainTex_TexelSize.xy), vec2(0.00000999999974738));

#line 140
    vec2 _S7 = vec2(texel_0.x, 0.0);

#line 140
    vec2 _S8 = vec2(0.0, texel_0.y);

#line 140
    return (luminance_1 - (GetLuminance_0(RU_tex2D_MainTex_0(uv_6 - _S7).xyz) + GetLuminance_0(RU_tex2D_MainTex_0(uv_6 + _S7).xyz) + GetLuminance_0(RU_tex2D_MainTex_0(uv_6 - _S8).xyz) + GetLuminance_0(RU_tex2D_MainTex_0(uv_6 + _S8).xyz)) * 0.25) * 2.0;
}


#line 140
float GetReliefShade_0(vec2 uv_7)
{

#line 140
    vec2 texel_1 = max(abs(RU_Material_0._MainTex_TexelSize.xy), vec2(0.00000999999974738));

#line 140
    vec2 _S9 = vec2(texel_1.x, 0.0);

#line 140
    vec2 _S10 = vec2(0.0, texel_1.y);

#line 140
    return mix(1.0, mix(0.81999999284744263, 1.12000000476837158, dot(normalize(vec3(- vec2(GetLuminance_0(RU_tex2D_MainTex_0(uv_7 + _S9).xyz) - GetLuminance_0(RU_tex2D_MainTex_0(uv_7 - _S9).xyz), GetLuminance_0(RU_tex2D_MainTex_0(uv_7 + _S10).xyz) - GetLuminance_0(RU_tex2D_MainTex_0(uv_7 - _S10).xyz)) * 3.0, 1.0)), normalize(vec3(RU_Material_0._ReliefLightDirection.xy, 1.0))) * 0.5 + 0.5), saturate_1(RU_Material_0._ReliefStrength));
}


#line 140
vec4 GetWindGrainLayer_0(vec2 uv_8, float dissolve_1, float radius_0, float layer_0, vec4 tint_0)
{

#line 140
    float seed_0 = 13.69999980926513672 + layer_0 * 31.89999961853027344;

#line 140
    vec2 _S11 = vec2(-1.75 + layer_0 * 0.2199999988079071, 1.45000004768371582 + layer_0 * 0.37999999523162842);

#line 140
    vec2 velocity_0 = _S11 * radius_0;

#line 140
    float _S12 = max(RU_Material_0._ParticleScale * (0.81999999284744263 + layer_0 * 0.12999999523162842), 1.0);

#line 140
    vec2 cell_1 = floor((uv_8 - velocity_0 * (dissolve_1 - GetPeelTime_0(uv_8)) / (1.0 - dot(velocity_0, vec2(0.43999999761581421, -0.56000000238418579) * 0.68000000715255737))) * _S12);

#line 140
    float randomSize_0 = Hash21_0(cell_1 + seed_0);

#line 140
    float randomLife_0 = Hash21_0(cell_1 + vec2(seed_0 * 2.1099998950958252, seed_0 * 0.73000001907348633));

#line 140
    vec2 sourceCenter_0 = (cell_1 + 0.5 + (vec2(Hash21_0(cell_1 + vec2(seed_0, 7.09999990463256836)), Hash21_0(cell_1 + vec2(19.29999923706054688, seed_0))) - 0.5) * RU_Material_0._ParticleJitter * 0.40000000596046448) / _S12;

#line 140
    float age_0 = dissolve_1 - GetPeelTime_0(sourceCenter_0);

#line 140
    float life_0 = mix(0.15000000596046448, 0.2800000011920929, randomLife_0);

#line 140
    vec2 local_1 = (uv_8 - (sourceCenter_0 + velocity_0 * age_0)) * _S12;

#line 140
    vec2 wind_0 = normalize(_S11);

#line 140
    float halfSize_0 = RU_Material_0._ParticleSize * mix(0.30000001192092896, 0.55000001192092896, randomSize_0);

#line 140
    float _S13 = max(abs(dot(local_1, wind_0)) / mix(1.0, 1.79999995231628418, randomLife_0), abs(dot(local_1, vec2(- wind_0.y, wind_0.x))));

#line 140
    float _S14 = max((fwidth((_S13))), 0.02500000037252903);

#line 140
    vec4 source_0 = SamplePawn_0(sourceCenter_0) * RU_Linear(RU_Material_0._Color) * tint_0;

#line 140
    float alpha_0 = source_0.w * (1.0 - smoothstep(halfSize_0 - _S14, halfSize_0 + _S14, _S13)) * (step(0.0, age_0) * (1.0 - smoothstep(life_0 * 0.34999999403953552, life_0, age_0)) * step(1.0 - RU_Material_0._ParticleDensity, Hash21_0(cell_1 + vec2(seed_0 * 5.23000001907348633, seed_0 * 6.28999996185302734))));

#line 140
    vec3 _S15 = source_0.xyz;

#line 140
    vec3 color_1 = GetSandColor_0(GetLuminance_0(_S15), mix(0.34999999403953552, 0.94999998807907104, randomSize_0));

#line 140
    return vec4(mix(color_1, color_1 * _S15, vec3(saturate_1(RU_Material_0._OriginalTint))) * (RU_Material_0._Brightness * mix(0.89999997615814209, 1.25, randomLife_0)) * alpha_0, alpha_0);
}


#line 140
vec4 GetWindGrains_0(vec2 uv_9, float dissolve_2, float radius_1, vec4 tint_1)
{

#line 140
    vec4 result_0 = vec4(ivec4(0));

#line 140
    int layer_1 = 0;

#line 140
    [[unroll]]
    for(;;)
    {

#line 140
        if(layer_1 < 3)
        {
        }
        else
        {

#line 140
            break;
        }

#line 140
        vec4 grain_1 = GetWindGrainLayer_0(uv_9, dissolve_2, radius_1, float(layer_1), tint_1);

#line 140
        result_0.xyz = result_0.xyz + grain_1.xyz * (1.0 - result_0.w);

#line 140
        result_0[3] = result_0[3] + grain_1.w * (1.0 - result_0.w);

#line 140
        layer_1 = layer_1 + 1;

#line 140
    }

#line 140
    return result_0;
}


#line 140
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 140
layout(location = 1) in vec2 i_rawUv_0; // @vertex TexCoord0 optional


#line 140
layout(location = 5) in vec2 i_meshUv_0; // @vertex TexCoord1 optional


#line 140
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 140
void RU_SourceMain()
{

#line 140
    float progress_0 = saturate_1(RU_Material_0._SandProgress);

#line 140
    float dissolve_3 = saturate_1((abs(progress_0 - 0.5) - RU_Material_0._SolidHold) / (0.5 - RU_Material_0._SolidHold));

#line 140
    vec4 source_1 = SamplePawn_0(i_rawUv_0) * RU_Linear(RU_Material_0._Color) * i_color_0;

#line 140
    float _S16 = source_1.w;

#line 140
    float grain_2 = GetGrain_0(i_rawUv_0);

#line 140
    float sourceInfluence_0 = saturate_1(_S16 * 2.0);

#line 140
    vec3 _S17 = source_1.xyz;

#line 140
    float luminance_2 = mix(0.41999998688697815, GetLuminance_0(_S17), sourceInfluence_0);

#line 140
    vec3 bodyColor_0 = GetSandColor_0(luminance_2, grain_2) * mix(1.0, GetReliefShade_0(saturate_0(i_rawUv_0)), sourceInfluence_0) * mix(1.0, mix(0.77999997138977051, 1.10000002384185791, grain_2), RU_Material_0._GrainAmount) * clamp(1.0 + GetSourceDetail_0(saturate_0(i_rawUv_0), luminance_2) * sourceInfluence_0 * RU_Material_0._DetailPreserve, 0.72000002861022949, 1.08000004291534424);

#line 140
    vec3 bodyColor_1 = mix(mix(bodyColor_0, bodyColor_0 * _S17, vec3(saturate_1(RU_Material_0._OriginalTint) * sourceInfluence_0)), RU_Linear(RU_Material_0._SandLight).xyz, vec3(saturate_1(smoothstep(0.89999997615814209, 1.0, grain_2) * RU_Material_0._SparkleAmount * 0.44999998807907104))) * RU_Material_0._Brightness;

#line 140
    float bodyAlpha_0 = _S16 * GetRemainingBody_0(i_rawUv_0, dissolve_3) * mix(1.0, saturate_1(0.72000002861022949 + grain_2 * 0.44999998807907104), RU_Material_0._GrainAmount * 0.44999998807907104);

#line 140
    vec4 _S18 = vec4(ivec4(0));

#line 140
    bool _S19;

#line 140
    if(dissolve_3 > 0.0)
    {

#line 140
        _S19 = dissolve_3 < 1.0;

#line 140
    }
    else
    {

#line 140
        _S19 = false;

#line 140
    }

#line 140
    vec4 grains_0;

#line 140
    if(_S19)
    {

#line 140
        float radius_2;

#line 140
        if(progress_0 < 0.5)
        {

#line 140
            radius_2 = RU_Material_0._GatherRadius;

#line 140
        }
        else
        {

#line 140
            radius_2 = RU_Material_0._ScatterRadius;

#line 140
        }

#line 140
        grains_0 = GetWindGrains_0(i_rawUv_0, dissolve_3, radius_2, i_color_0);

#line 140
    }
    else
    {

#line 140
        grains_0 = _S18;

#line 140
    }

#line 140
    float _S20 = 1.0 - bodyAlpha_0;

#line 140
    float alpha_1 = bodyAlpha_0 + grains_0.w * _S20;

#line 140
    vec3 premultiplied_0 = bodyColor_1 * bodyAlpha_0 + grains_0.xyz * _S20;

#line 140
    vec2 edge_0 = min(i_meshUv_0, 1.0 - i_meshUv_0);

#line 140
    float alpha_2 = alpha_1 * (smoothstep(0.0, 0.03500000014901161, min(edge_0.x, edge_0.y)) * (1.0 - smoothstep(0.93999999761581421, 1.0, dissolve_3)));

#line 140
    clip_0(alpha_2 - RU_Material_0._AlphaCutoff * 0.34999999403953552);

#line 140
    entryPointParam_frag_0 = vec4(premultiplied_0 / max(alpha_1, 0.00009999999747379), alpha_2);

#line 140
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
