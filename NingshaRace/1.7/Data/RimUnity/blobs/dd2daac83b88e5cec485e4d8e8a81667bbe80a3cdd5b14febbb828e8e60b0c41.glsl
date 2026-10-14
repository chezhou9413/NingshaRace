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
    vec4 _CloudSand; // @default = vec4(0.23, 0.18, 0.115, 1) @color
    vec4 _DeepSand; // @default = vec4(0.13, 0.105, 0.075, 1) @color
    float _FlowTime; // @default = 0
    vec4 _GrainSand; // @default = vec4(0.83, 0.69, 0.46, 1) @color
};


#line 62
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _CloudSand; // @default = vec4(0.23, 0.18, 0.115, 1) @color
    vec4 _DeepSand; // @default = vec4(0.13, 0.105, 0.075, 1) @color
    float _FlowTime; // @default = 0
    vec4 _GrainSand; // @default = vec4(0.83, 0.69, 0.46, 1) @color
    float _RimUnitySrgb_GrainTex; // @default = 1
    float _RimUnitySrgb_NoiseTex; // @default = 1
}RU_Material_0;

#line 79
layout(binding = 3, set = 2)
uniform sampler2D _NoiseTex; // @default = gray


#line 81



#line 68
layout(binding = 1, set = 2)
uniform sampler2D _GrainTex; // @default = black


#line 70



#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 83 0
vec4 RU_tex2D_NoiseTex_0(vec2 uv_0)
{

#line 83
    return (RU_SampleLinear(texture(_NoiseTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_NoiseTex));
}


#line 91
vec2 StormWarp_0(vec2 uv_1)
{

#line 91
    vec2 slowWind_0 = vec2(-0.02199999988079071, 0.01400000043213367) * RU_Material_0._FlowTime;

#line 91
    return (RU_tex2D_NoiseTex_0(uv_1 + slowWind_0).xy - 0.5) * 0.2199999988079071 + (RU_tex2D_NoiseTex_0(uv_1 * 2.0 + slowWind_0 * 1.70000004768371582 + 0.37000000476837158).zw - 0.5) * 0.05499999970197678;
}


#line 91
float StormNoise_0(vec2 uv_2)
{

#line 91
    return dot(RU_tex2D_NoiseTex_0(uv_2), vec4(0.47999998927116394, 0.2800000011920929, 0.15999999642372131, 0.07999999821186066));
}


#line 91
float StormDensity_0(vec2 uv_3, vec2 wind_0, vec2 warp_0, out float billow_0, out float shreds_0)
{

#line 91
    float body_0 = StormNoise_0(uv_3 + wind_0 + warp_0);

#line 91
    vec4 breaking_0 = RU_tex2D_NoiseTex_0(uv_3 * 2.0 + wind_0 * 1.46000003814697266 - warp_0 * 0.60000002384185791 + 0.18999999761581421);

#line 91
    billow_0 = smoothstep(0.23000000417232513, 0.72000002861022949, body_0);

#line 91
    float _S1 = smoothstep(0.2800000011920929, 0.73000001907348633, breaking_0.z + (breaking_0.w - 0.5) * 0.31999999284744263);

#line 91
    shreds_0 = _S1;

#line 91
    return saturate_0(billow_0 * (0.40000000596046448 + smoothstep(0.2199999988079071, 0.69999998807907104, body_0 * 0.64999997615814209 + breaking_0.y * 0.34999999403953552) * 0.60000002384185791) + _S1 * 0.17000000178813934);
}


#line 72
vec4 RU_tex2D_GrainTex_0(vec2 uv_4)
{

#line 72
    return (RU_SampleLinear(texture(_GrainTex, (vec2(uv_4.x, 1.0 - uv_4.y))), RU_Material_0._RimUnitySrgb_GrainTex));
}


#line 91
vec3 GrainField_0(vec2 uv_5, vec2 wind_1, vec2 warp_1)
{

#line 91
    vec2 coarseUv_0 = uv_5 + wind_1 * 0.82999998331069946 - warp_1 * 0.18999999761581421 + vec2(0.40999999642372131, 0.27000001072883606);

#line 91
    float _S2 = coarseUv_0.x;

#line 91
    float _S3 = coarseUv_0.y;

#line 91
    vec3 fine_0 = RU_tex2D_GrainTex_0(uv_5 + wind_1 * 1.35000002384185791 + warp_1 * 0.31999999284744263).xyz;

#line 91
    return vec3(fine_0.x, RU_tex2D_GrainTex_0(vec2(_S2 + _S3, _S3 - _S2)).y, fine_0.z);
}


#line 91
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 91
layout(location = 1) in vec2 input_uv_0; // @vertex TexCoord0 optional


#line 91
void RU_SourceMain()
{

#line 91
    vec2 wind_2 = vec2(-0.05799999833106995, 0.01200000010430813) * RU_Material_0._FlowTime;

#line 91
    vec2 warp_2 = StormWarp_0(input_uv_0);

#line 91
    float billow_1;

#line 91
    float shreds_1;

#line 91
    float density_0 = StormDensity_0(input_uv_0, wind_2, warp_2, billow_1, shreds_1);

#line 91
    vec3 grain_0 = GrainField_0(input_uv_0, wind_2, warp_2);

#line 91
    float grainLight_0 = saturate_0(grain_0.x * 0.80000001192092896 + grain_0.y * 0.2800000011920929) * mix(0.64999997615814209, 1.0, density_0);

#line 91
    entryPointParam_frag_0 = vec4(mix(mix(RU_Linear(RU_Material_0._DeepSand).xyz, RU_Linear(RU_Material_0._CloudSand).xyz, vec3(saturate_0(0.30000001192092896 + density_0 * 0.57999998331069946 + (RU_tex2D_NoiseTex_0(input_uv_0 * 4.0 + wind_2 * 2.09999990463256836 + warp_2).w - 0.5) * 0.09000000357627869))) * (1.0 + (grain_0.z - 0.5) * 0.14000000059604645) + RU_Linear(RU_Material_0._CloudSand).xyz * shreds_1 * 0.02500000037252903, RU_Linear(RU_Material_0._GrainSand).xyz, vec3(grainLight_0 * 0.62000000476837158)), 0.01999999955296516 + density_0 * 0.15999999642372131 + grainLight_0 * 0.10000000149011612);

#line 91
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
