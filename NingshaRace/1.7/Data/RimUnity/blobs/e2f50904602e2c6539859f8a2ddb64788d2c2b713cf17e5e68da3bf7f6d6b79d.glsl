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
    float _Alpha; // @default = 1
    float _CenterOpacity; // @default = 0.055
    vec4 _CoreColor; // @default = vec4(0.12, 0.035, 0.006, 1) @color
    float _EdgeSoftness; // @default = 0.025
    float _FlowStretch; // @default = 7
    float _GlowIntensity; // @default = 1.7
    vec4 _HotColor; // @default = vec4(1, 0.82, 0.31, 1) @color
    float _LensConvexity; // @default = 1.25
    float _OuterStorm; // @default = 1.15
    vec4 _RimColor; // @default = vec4(1, 0.52, 0.08, 1) @color
    float _RimPower; // @default = 2.4
    float _RimWidth; // @default = 0.23
    float _SandAmount; // @default = 1.25
    vec4 _SandColor; // @default = vec4(0.72, 0.29, 0.035, 1) @color
    float _SandScale; // @default = 24
    float _Seed; // @default = 0
    float _ShieldStrength; // @default = 1
    float _SphereRadius; // @default = 0.88
    float _SphereSquash; // @default = 1.06
    float _StreakSharpness; // @default = 2.1
    vec4 _Time;
    float _Turbulence; // @default = 0.85
    float _WindSpeed; // @default = 1.35
};


#line 81
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _Alpha; // @default = 1
    float _CenterOpacity; // @default = 0.055
    vec4 _CoreColor; // @default = vec4(0.12, 0.035, 0.006, 1) @color
    float _EdgeSoftness; // @default = 0.025
    float _FlowStretch; // @default = 7
    float _GlowIntensity; // @default = 1.7
    vec4 _HotColor; // @default = vec4(1, 0.82, 0.31, 1) @color
    float _LensConvexity; // @default = 1.25
    float _OuterStorm; // @default = 1.15
    vec4 _RimColor; // @default = vec4(1, 0.52, 0.08, 1) @color
    float _RimPower; // @default = 2.4
    float _RimWidth; // @default = 0.23
    float _SandAmount; // @default = 1.25
    vec4 _SandColor; // @default = vec4(0.72, 0.29, 0.035, 1) @color
    float _SandScale; // @default = 24
    float _Seed; // @default = 0
    float _ShieldStrength; // @default = 1
    float _SphereRadius; // @default = 0.88
    float _SphereSquash; // @default = 1.06
    float _StreakSharpness; // @default = 2.1
    vec4 _Time;
    float _Turbulence; // @default = 0.85
    float _WindSpeed; // @default = 1.35
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


#line 107 0
vec3 GetSphereNormal_0(vec2 centeredUv_0)
{

#line 107
    vec2 spherePoint_0 = centeredUv_0 / max(RU_Material_0._SphereRadius, 0.00100000004749745);

#line 107
    return normalize(vec3(spherePoint_0, max(pow(sqrt(saturate_0(1.0 - dot(spherePoint_0, spherePoint_0))), max(RU_Material_0._LensConvexity, 0.00999999977648258)), 0.00100000004749745)));
}


#line 107
vec2 GetSurfaceCoordinates_0(vec3 sphereNormal_0)
{

#line 107
    return vec2((atan((sphereNormal_0.x),(max(sphereNormal_0.z, 0.00100000004749745)))) / 3.14159274101257324, asin(clamp(sphereNormal_0.y, -1.0, 1.0)) / 3.14159274101257324);
}


#line 107
float Hash21_0(vec2 value_0)
{

#line 107
    vec2 _S1 = fract(value_0 * vec2(123.339996337890625, 456.209991455078125));

#line 107
    vec2 _S2 = _S1 + dot(_S1, _S1 + 45.31999969482421875 + RU_Material_0._Seed);

#line 107
    return fract(_S2.x * _S2.y);
}


#line 107
float ValueNoise_0(vec2 uv_0)
{

#line 107
    vec2 cell_0 = floor(uv_0);

#line 107
    vec2 local_0 = fract(uv_0);

#line 107
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 107
    float _S3 = blend_0.x;

#line 107
    return mix(mix(Hash21_0(cell_0), Hash21_0(cell_0 + vec2(1.0, 0.0)), _S3), mix(Hash21_0(cell_0 + vec2(0.0, 1.0)), Hash21_0(cell_0 + vec2(1.0, 1.0)), _S3), blend_0.y);
}


#line 107
float FractalNoise_0(vec2 uv_1)
{

#line 107
    return ValueNoise_0(uv_1) * 0.56000000238418579 + ValueNoise_0(uv_1 * 2.02999997138977051 + 17.30999946594238281) * 0.28999999165534973 + ValueNoise_0(uv_1 * 4.1100001335144043 + 43.1699981689453125) * 0.15000000596046448;
}


#line 107
vec2 GetSurfaceSand_0(vec3 sphereNormal_1, float time_0, float strength01_0)
{

#line 107
    vec2 surfaceUv_0 = GetSurfaceCoordinates_0(sphereNormal_1);

#line 107
    float _S4 = surfaceUv_0.x;

#line 107
    float _S5 = surfaceUv_0.y;

#line 107
    float broadNoise_0 = FractalNoise_0(vec2(_S4 * RU_Material_0._SandScale * 0.20000000298023224 - time_0 * RU_Material_0._WindSpeed * 0.55000001192092896, _S5 * RU_Material_0._SandScale * 0.47999998927116394) + RU_Material_0._Seed * 0.10999999940395355);

#line 107
    float bend_0 = sin(_S4 * 13.0 - time_0 * RU_Material_0._WindSpeed * 1.60000002384185791 + broadNoise_0 * 3.0);

#line 107
    float _S6 = max(RU_Material_0._FlowStretch, 1.0);

#line 107
    vec2 primaryUv_0 = vec2(_S4 * RU_Material_0._SandScale / _S6 - time_0 * RU_Material_0._WindSpeed * 1.25, _S5 * RU_Material_0._SandScale + bend_0 * RU_Material_0._Turbulence * 0.64999997615814209);

#line 107
    float secondaryNoise_0 = FractalNoise_0(vec2(_S4 * RU_Material_0._SandScale / max(_S6 * 0.57999998331069946, 1.0) - time_0 * RU_Material_0._WindSpeed * 1.8200000524520874 + 23.70000076293945312, _S5 * RU_Material_0._SandScale * 1.73000001907348633 - bend_0 * RU_Material_0._Turbulence * 0.37999999523162842 + 11.39999961853027344));

#line 107
    float flowNoise_0 = FractalNoise_0(primaryUv_0) * 0.68000000715255737 + secondaryNoise_0 * 0.31999999284744263;

#line 107
    float noiseThreshold_0 = mix(0.68000000715255737, 0.43000000715255737, strength01_0);

#line 107
    return vec2((pow(saturate_0(smoothstep(noiseThreshold_0, 0.89999997615814209, flowNoise_0)), max(RU_Material_0._StreakSharpness, 0.00999999977648258)) * (0.72000002861022949 + smoothstep(0.2800000011920929, 0.77999997138977051, FractalNoise_0(primaryUv_0 * 0.4699999988079071 + secondaryNoise_0 * 2.29999995231628418)) * 1.04999995231628418) + smoothstep(noiseThreshold_0, 0.92000001668930054, broadNoise_0) * flowNoise_0 * 0.41999998688697815) * mix(0.37999999523162842, 1.0, saturate_0(sphereNormal_1.z)) * RU_Material_0._SandAmount, broadNoise_0);
}


#line 107
float PeriodicValueNoise_0(vec2 uv_2, float periodLength_0)
{

#line 107
    float _S7 = max(floor(periodLength_0 + 0.5), 1.0);

#line 107
    vec2 cell_1 = floor(uv_2);

#line 107
    vec2 local_1 = fract(uv_2);

#line 107
    vec2 blend_1 = local_1 * local_1 * (3.0 - 2.0 * local_1);

#line 107
    float _S8 = cell_1.x;

#line 107
    float currentX_0 = _S8 - floor(_S8 / _S7) * _S7;

#line 107
    float nextCellX_0 = _S8 + 1.0;

#line 107
    float nextX_0 = nextCellX_0 - floor(nextCellX_0 / _S7) * _S7;

#line 107
    float _S9 = cell_1.y;

#line 107
    float _S10 = _S9 + 1.0;

#line 107
    float _S11 = blend_1.x;

#line 107
    return mix(mix(Hash21_0(vec2(currentX_0, _S9)), Hash21_0(vec2(nextX_0, _S9)), _S11), mix(Hash21_0(vec2(currentX_0, _S10)), Hash21_0(vec2(nextX_0, _S10)), _S11), blend_1.y);
}


#line 107
float PeriodicFractalNoise_0(vec2 uv_3, float periodLength_1)
{

#line 107
    return PeriodicValueNoise_0(uv_3, periodLength_1) * 0.56000000238418579 + PeriodicValueNoise_0(uv_3 * 2.0 + 17.30999946594238281, periodLength_1 * 2.0) * 0.28999999165534973 + PeriodicValueNoise_0(uv_3 * 4.0 + 43.1699981689453125, periodLength_1 * 4.0) * 0.15000000596046448;
}


#line 107
float GetOuterStorm_0(vec2 centeredUv_1, float radius_0, float time_1, float strength01_1)
{

#line 107
    float shellDistance_0 = abs(radius_0 - RU_Material_0._SphereRadius);

#line 107
    float _S12 = max(floor(RU_Material_0._SandScale * 0.68000000715255737 + 0.5), 1.0);

#line 107
    return (1.0 - smoothstep(0.01499999966472387, 0.17000000178813934 + strength01_1 * 0.03500000014901161, shellDistance_0)) * smoothstep(mix(0.72000002861022949, 0.47999998927116394, strength01_1), 0.93999999761581421, PeriodicFractalNoise_0(vec2(((atan((centeredUv_1.y),(centeredUv_1.x))) / 6.28318548202514648 + 0.5) * _S12 - time_1 * RU_Material_0._WindSpeed * 0.46000000834465027, shellDistance_0 * RU_Material_0._SandScale * 1.70000004768371582 - time_1 * 0.30000001192092896), _S12)) * RU_Material_0._OuterStorm;
}


#line 107
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 107
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 107
void RU_SourceMain()
{

#line 107
    float strength_0 = clamp(RU_Material_0._ShieldStrength, 0.0, 2.0);

#line 107
    float strength01_2 = saturate_0(strength_0);

#line 107
    float overcharge_0 = saturate_0(strength_0 - 1.0);

#line 107
    float time_2 = RU_Material_0._Time.y + RU_Material_0._Seed * 5.17000007629394531;

#line 107
    vec2 centeredUv_2 = (i_uv_0 - 0.5) * 2.0;

#line 107
    centeredUv_2[1] = centeredUv_2[1] * RU_Material_0._SphereSquash;

#line 107
    float radius_1 = length(centeredUv_2);

#line 107
    float sphereMask_0 = 1.0 - smoothstep(RU_Material_0._SphereRadius - RU_Material_0._EdgeSoftness, RU_Material_0._SphereRadius + RU_Material_0._EdgeSoftness, radius_1);

#line 107
    float outerCoverage_0 = 1.0 - smoothstep(RU_Material_0._SphereRadius + 0.07999999821186066, RU_Material_0._SphereRadius + 0.23999999463558197, radius_1);

#line 107
    clip_0(outerCoverage_0 * strength_0 - 0.00100000004749745);

#line 107
    vec3 sphereNormal_2 = GetSphereNormal_0(centeredUv_2);

#line 107
    float _S13 = 1.0 - saturate_0(sphereNormal_2.z);

#line 107
    float rim_0 = max(pow(saturate_0(_S13), max(RU_Material_0._RimPower, 0.00999999977648258)), smoothstep(1.0 - RU_Material_0._RimWidth, 1.0, _S13) * 0.62000000476837158) * sphereMask_0;

#line 107
    vec2 sandData_0 = GetSurfaceSand_0(sphereNormal_2, time_2, strength01_2);

#line 107
    float sand_0 = sandData_0.x * sphereMask_0;

#line 107
    float broadNoise_1 = sandData_0.y;

#line 107
    float outerStorm_0 = GetOuterStorm_0(centeredUv_2, radius_1, time_2, strength01_2) * outerCoverage_0;

#line 107
    float centerMist_0 = (RU_Material_0._CenterOpacity + broadNoise_1 * 0.08500000089406967) * sphereMask_0 * (0.34999999403953552 + strength01_2 * 0.64999997615814209);

#line 107
    float hotSand_0 = saturate_0(sand_0 * 0.77999997138977051 + overcharge_0 * broadNoise_1);

#line 107
    float alpha_0 = saturate_0((centerMist_0 + sand_0 * 0.34000000357627869 + rim_0 * (0.31000000238418579 + strength01_2 * 0.20000000298023224) + outerStorm_0 * 0.25999999046325684) * (RU_Material_0._Alpha * saturate_0(strength_0 * 1.60000002384185791)));

#line 107
    entryPointParam_frag_0 = vec4((mix(RU_Linear(RU_Material_0._CoreColor).xyz, RU_Linear(RU_Material_0._SandColor).xyz, vec3(saturate_0(broadNoise_1 + sand_0 * 0.2800000011920929))) * (centerMist_0 + sand_0 * 0.46000000834465027 + outerStorm_0 * 0.34999999403953552) + RU_Linear(RU_Material_0._RimColor).xyz * (rim_0 * (0.64999997615814209 + strength_0 * 0.46000000834465027) + outerStorm_0 * 0.31999999284744263) + RU_Linear(RU_Material_0._HotColor).xyz * hotSand_0 * RU_Material_0._GlowIntensity) * alpha_0 + RU_Linear(RU_Material_0._HotColor).xyz * (hotSand_0 * 0.2199999988079071 + rim_0 * overcharge_0 * 0.44999998807907104) * RU_Material_0._GlowIntensity * alpha_0, alpha_0);

#line 107
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
