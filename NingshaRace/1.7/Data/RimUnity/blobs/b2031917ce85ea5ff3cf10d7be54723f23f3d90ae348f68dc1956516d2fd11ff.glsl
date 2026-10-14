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


#line 110 0
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 112



#line 56
struct RU_MaterialData_0
{
    float _Alpha; // @default = 0.72
    float _Brightness; // @default = 0.82
    float _Dissolve; // @default = 0.34
    float _DissolveSoftness; // @default = 0.2
    float _EdgeBreakup; // @default = 0.24
    float _EdgeSoftness; // @default = 0.42
    float _FadeIn; // @default = 0.08
    float _FadeOut; // @default = 0.38
    float _FlowSpeed; // @default = 0.68
    float _FlowStretch; // @default = 4.2
    float _HighlightStrength; // @default = 0.16
    float _InvFade; // @default = 1.6
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseContrast; // @default = 1.35
    float _NoiseInfluence; // @default = 0.82
    float _NoiseScale; // @default = 6.4
    vec4 _ProjectionParams;
    float _Seed; // @default = 0
    float _ShadowStrength; // @default = 0.46
    float _TextureColorStrength; // @default = 0
    vec4 _Time;
    float _Turbulence; // @default = 0.78
    vec4 _WindDirection; // @default = vec4(1, 0.18, 0, 0)
    vec4 _ZBufferParams;
    vec4 unity_OrthoParams;
};


#line 83
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _Alpha; // @default = 0.72
    float _Brightness; // @default = 0.82
    float _Dissolve; // @default = 0.34
    float _DissolveSoftness; // @default = 0.2
    float _EdgeBreakup; // @default = 0.24
    float _EdgeSoftness; // @default = 0.42
    float _FadeIn; // @default = 0.08
    float _FadeOut; // @default = 0.38
    float _FlowSpeed; // @default = 0.68
    float _FlowStretch; // @default = 4.2
    float _HighlightStrength; // @default = 0.16
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseContrast; // @default = 1.35
    float _NoiseInfluence; // @default = 0.82
    float _NoiseScale; // @default = 6.4
    float _Seed; // @default = 0
    float _ShadowStrength; // @default = 0.46
    float _TextureColorStrength; // @default = 0
    vec4 _Time;
    float _Turbulence; // @default = 0.78
    vec4 _WindDirection; // @default = vec4(1, 0.18, 0, 0)
    float _RimUnitySrgb_MainTex; // @default = 1
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 114 0
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 114
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 122
float Hash21_0(vec2 value_0)
{

#line 122
    vec2 _S1 = fract(value_0 * vec2(123.339996337890625, 456.209991455078125));

#line 122
    vec2 _S2 = _S1 + dot(_S1, _S1 + 45.31999969482421875 + RU_Material_0._Seed);

#line 122
    return fract(_S2.x * _S2.y);
}


#line 122
float ValueNoise_0(vec2 uv_1)
{

#line 122
    vec2 cell_0 = floor(uv_1);

#line 122
    vec2 local_0 = fract(uv_1);

#line 122
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 122
    float _S3 = blend_0.x;

#line 122
    return mix(mix(Hash21_0(cell_0), Hash21_0(cell_0 + vec2(1.0, 0.0)), _S3), mix(Hash21_0(cell_0 + vec2(0.0, 1.0)), Hash21_0(cell_0 + vec2(1.0, 1.0)), _S3), blend_0.y);
}


#line 122
float FractalNoise_0(vec2 uv_2)
{

#line 122
    return ValueNoise_0(uv_2) * 0.56000000238418579 + ValueNoise_0(uv_2 * 2.02999997138977051 + 17.30999946594238281) * 0.28999999165534973 + ValueNoise_0(uv_2 * 4.1100001335144043 + 43.1699981689453125) * 0.15000000596046448;
}


#line 122
vec2 GetFlowingDust_0(vec2 centeredUv_0, float time_0)
{

#line 122
    vec2 wind_0 = RU_Material_0._WindDirection.xy;

#line 122
    vec2 wind_1 = wind_0 * (inversesqrt((max(dot(wind_0, wind_0), 0.00009999999747379))));

#line 122
    float alongWind_0 = dot(centeredUv_0, wind_1);

#line 122
    float acrossWind_0 = dot(centeredUv_0, vec2(- wind_1.y, wind_1.x));

#line 122
    float _S4 = max(RU_Material_0._FlowStretch, 1.0);

#line 122
    float broadNoise_0 = FractalNoise_0(vec2(alongWind_0 * RU_Material_0._NoiseScale / _S4 - time_0 * RU_Material_0._FlowSpeed * 0.41999998688697815, acrossWind_0 * RU_Material_0._NoiseScale * 0.51999998092651367 + time_0 * RU_Material_0._FlowSpeed * 0.03500000014901161) + RU_Material_0._Seed * 0.07100000232458115);

#line 122
    float warp_0 = (broadNoise_0 - 0.5) * RU_Material_0._Turbulence;

#line 122
    return vec2(pow(saturate_0(broadNoise_0 * 0.37999999523162842 + FractalNoise_0(vec2(alongWind_0 * RU_Material_0._NoiseScale / _S4 - time_0 * RU_Material_0._FlowSpeed, acrossWind_0 * RU_Material_0._NoiseScale + warp_0 * 3.09999990463256836) + 13.69999980926513672) * 0.43999999761581421 + FractalNoise_0(vec2(alongWind_0 * RU_Material_0._NoiseScale / max(_S4 * 0.62000000476837158, 1.0) - time_0 * RU_Material_0._FlowSpeed * 1.47000002861022949 + 27.39999961853027344, acrossWind_0 * RU_Material_0._NoiseScale * 1.62999999523162842 - warp_0 * 1.79999995231628418 + time_0 * RU_Material_0._FlowSpeed * 0.07999999821186066)) * 0.18000000715255737), max(RU_Material_0._NoiseContrast, 0.00999999977648258)), broadNoise_0);
}


#line 122
float GetLifetimeFade_0(float age_0)
{

#line 122
    return smoothstep(0.0, max(RU_Material_0._FadeIn, 0.00009999999747379), age_0) * (1.0 - smoothstep(1.0 - max(RU_Material_0._FadeOut, 0.00009999999747379), 1.0, age_0));
}


#line 122
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 122
layout(location = 1) in vec4 i_uv_0; // @vertex TexCoord0 optional


#line 122
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 122
layout(location = 5) in float i_age_0; // @vertex TexCoord1 optional


#line 122
void RU_SourceMain()
{

#line 122
    vec4 textureSample_0 = RU_tex2D_MainTex_0(i_uv_0.xy);

#line 122
    vec2 centeredUv_1 = (i_uv_0.zw - 0.5) * 2.0;

#line 122
    vec2 dustData_0 = GetFlowingDust_0(centeredUv_1, RU_Material_0._Time.y + RU_Material_0._Seed * 2.17000007629394531);

#line 122
    float dustNoise_0 = dustData_0.x;

#line 122
    float broadNoise_1 = dustData_0.y;

#line 122
    float _S5 = max(RU_Material_0._DissolveSoftness * 0.5, 0.00499999988824129);

#line 122
    entryPointParam_frag_0 = vec4(i_color_0.xyz * (mix(1.0 - RU_Material_0._ShadowStrength, 1.0, saturate_0(broadNoise_1 * 0.77999997138977051 + dustNoise_0 * 0.51999998092651367)) + pow(saturate_0(dustNoise_0), 3.0) * RU_Material_0._HighlightStrength) * mix(vec3(1.0), textureSample_0.xyz, vec3(RU_Material_0._TextureColorStrength)) * RU_Material_0._Brightness, saturate_0(textureSample_0.w * (1.0 - smoothstep(max(1.0 - RU_Material_0._EdgeSoftness, 0.0), 1.0, length(centeredUv_1) + (mix(broadNoise_1, dustNoise_0, 0.41999998688697815) - 0.5) * RU_Material_0._EdgeBreakup)) * mix(1.0, smoothstep(RU_Material_0._Dissolve - _S5, RU_Material_0._Dissolve + _S5, dustNoise_0), RU_Material_0._NoiseInfluence) * (i_color_0.w * RU_Material_0._Alpha * GetLifetimeFade_0(i_age_0))));

#line 122
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
