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
    float _AngularDensity; // @default = 18
    float _BurstStrength; // @default = 1
    vec4 _CoreColor; // @default = vec4(0.1, 0.025, 0.004, 1) @color
    float _EdgeSoftness; // @default = 0.035
    float _EndRadius; // @default = 0.9
    float _FlowSpeed; // @default = 1.45
    float _GlowIntensity; // @default = 1.65
    vec4 _HotColor; // @default = vec4(1, 0.78, 0.25, 1) @color
    float _NoiseContrast; // @default = 1.8
    float _Progress; // @default = 0
    float _RadialDensity; // @default = 26
    float _RadialFlow; // @default = 0.38
    float _RingWidth; // @default = 0.13
    float _SandAmount; // @default = 1.35
    vec4 _SandColor; // @default = vec4(0.72, 0.25, 0.025, 1) @color
    float _Seed; // @default = 0
    float _StartRadius; // @default = 0.08
    vec4 _Time;
    float _TrailLength; // @default = 0.28
    float _Turbulence; // @default = 0.82
};


#line 79
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _Alpha; // @default = 1
    float _AngularDensity; // @default = 18
    float _BurstStrength; // @default = 1
    vec4 _CoreColor; // @default = vec4(0.1, 0.025, 0.004, 1) @color
    float _EdgeSoftness; // @default = 0.035
    float _EndRadius; // @default = 0.9
    float _FlowSpeed; // @default = 1.45
    float _GlowIntensity; // @default = 1.65
    vec4 _HotColor; // @default = vec4(1, 0.78, 0.25, 1) @color
    float _NoiseContrast; // @default = 1.8
    float _Progress; // @default = 0
    float _RadialDensity; // @default = 26
    float _RadialFlow; // @default = 0.38
    float _RingWidth; // @default = 0.13
    float _SandAmount; // @default = 1.35
    vec4 _SandColor; // @default = vec4(0.72, 0.25, 0.025, 1) @color
    float _Seed; // @default = 0
    float _StartRadius; // @default = 0.08
    vec4 _Time;
    float _TrailLength; // @default = 0.28
    float _Turbulence; // @default = 0.82
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 103 0
float Hash21_0(vec2 value_0)
{

#line 103
    vec2 _S1 = fract(value_0 * vec2(123.339996337890625, 456.209991455078125));

#line 103
    vec2 _S2 = _S1 + dot(_S1, _S1 + 45.31999969482421875 + RU_Material_0._Seed);

#line 103
    return fract(_S2.x * _S2.y);
}


#line 103
float PeriodicValueNoise_0(vec2 uv_0, float periodLength_0)
{

#line 103
    float _S3 = max(floor(periodLength_0 + 0.5), 1.0);

#line 103
    vec2 cell_0 = floor(uv_0);

#line 103
    vec2 local_0 = fract(uv_0);

#line 103
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 103
    float _S4 = cell_0.x;

#line 103
    float currentX_0 = _S4 - floor(_S4 / _S3) * _S3;

#line 103
    float nextCellX_0 = _S4 + 1.0;

#line 103
    float nextX_0 = nextCellX_0 - floor(nextCellX_0 / _S3) * _S3;

#line 103
    float _S5 = cell_0.y;

#line 103
    float _S6 = _S5 + 1.0;

#line 103
    float _S7 = blend_0.x;

#line 103
    return mix(mix(Hash21_0(vec2(currentX_0, _S5)), Hash21_0(vec2(nextX_0, _S5)), _S7), mix(Hash21_0(vec2(currentX_0, _S6)), Hash21_0(vec2(nextX_0, _S6)), _S7), blend_0.y);
}


#line 103
float PeriodicFractalNoise_0(vec2 uv_1, float periodLength_1)
{

#line 103
    return PeriodicValueNoise_0(uv_1, periodLength_1) * 0.56000000238418579 + PeriodicValueNoise_0(uv_1 * 2.0 + 17.30999946594238281, periodLength_1 * 2.0) * 0.28999999165534973 + PeriodicValueNoise_0(uv_1 * 4.0 + 43.1699981689453125, periodLength_1 * 4.0) * 0.15000000596046448;
}


#line 103
vec3 GetBurstSand_0(vec2 centeredUv_0, float radius_0, float ringRadius_0, float time_0, float strength01_0)
{

#line 103
    float angle01_0 = (atan((centeredUv_0.y),(centeredUv_0.x))) / 6.28318548202514648 + 0.5;

#line 103
    float _S8 = max(floor(RU_Material_0._AngularDensity + 0.5), 1.0);

#line 103
    float radialOffset_0 = radius_0 - ringRadius_0;

#line 103
    float _S9 = angle01_0 * _S8;

#line 103
    float broadNoise_0 = PeriodicFractalNoise_0(vec2(_S9 - time_0 * RU_Material_0._FlowSpeed * 0.31999999284744263, radialOffset_0 * RU_Material_0._RadialDensity * 0.27000001072883606 - time_0 * RU_Material_0._RadialFlow * 0.34999999403953552), _S8);

#line 103
    float warpedRadial_0 = radialOffset_0 * RU_Material_0._RadialDensity + (broadNoise_0 - 0.5) * RU_Material_0._Turbulence * 4.19999980926513672;

#line 103
    float secondaryPeriod_0 = _S8 * 2.0;

#line 103
    float flowNoise_0 = PeriodicFractalNoise_0(vec2(_S9 - time_0 * RU_Material_0._FlowSpeed, warpedRadial_0 - time_0 * RU_Material_0._RadialFlow), _S8) * 0.68000000715255737 + PeriodicFractalNoise_0(vec2(angle01_0 * secondaryPeriod_0 - time_0 * RU_Material_0._FlowSpeed * 1.62999999523162842 + 19.70000076293945312, warpedRadial_0 * 1.74000000953674316 + time_0 * RU_Material_0._RadialFlow * 0.46000000834465027 + 11.30000019073486328), secondaryPeriod_0) * 0.31999999284744263;

#line 103
    float ringDistance_0 = abs(radialOffset_0);

#line 103
    float _S10 = max(ringRadius_0 - radius_0, 0.0);

#line 103
    return vec3(((1.0 - smoothstep(RU_Material_0._RingWidth, RU_Material_0._RingWidth + RU_Material_0._EdgeSoftness, ringDistance_0)) * (0.23999999463558197 + pow(saturate_0(smoothstep(mix(0.68000000715255737, 0.43000000715255737, strength01_0), 0.92000001668930054, flowNoise_0)), max(RU_Material_0._NoiseContrast, 0.00999999977648258)) * 1.20000004768371582) + step(radius_0, ringRadius_0) * smoothstep(0.0, max(RU_Material_0._EdgeSoftness, 0.00100000004749745), _S10) * (1.0 - smoothstep(RU_Material_0._TrailLength * 0.55000001192092896, RU_Material_0._TrailLength, _S10)) * smoothstep(0.37999999523162842, 0.86000001430511475, broadNoise_0 + flowNoise_0 * 0.2800000011920929) * (0.37999999523162842 + flowNoise_0 * 0.72000002861022949)) * RU_Material_0._SandAmount, (1.0 - smoothstep(RU_Material_0._RingWidth * 0.25999999046325684, RU_Material_0._RingWidth * 0.25999999046325684 + RU_Material_0._EdgeSoftness, ringDistance_0)) * smoothstep(0.46000000834465027, 0.89999997615814209, flowNoise_0), broadNoise_0);
}


#line 103
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 103
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 103
void RU_SourceMain()
{

#line 103
    float progress_0 = saturate_0(RU_Material_0._Progress);

#line 103
    float strength_0 = clamp(RU_Material_0._BurstStrength, 0.0, 2.0);

#line 103
    vec2 centeredUv_1 = (i_uv_0 - 0.5) * 2.0;

#line 103
    vec3 sandData_0 = GetBurstSand_0(centeredUv_1, length(centeredUv_1), mix(RU_Material_0._StartRadius, RU_Material_0._EndRadius, progress_0 * progress_0 * (3.0 - 2.0 * progress_0)), RU_Material_0._Time.y + RU_Material_0._Seed * 5.17000007629394531, saturate_0(strength_0));

#line 103
    float density_0 = sandData_0.x;

#line 103
    float hot_0 = sandData_0.y;

#line 103
    float alpha_0 = saturate_0((density_0 * 0.57999998331069946 + hot_0 * 0.23999999463558197) * (smoothstep(0.0, 0.03500000014901161, progress_0) * (1.0 - smoothstep(0.72000002861022949, 1.0, progress_0)) * RU_Material_0._Alpha * saturate_0(strength_0 * 1.5)));

#line 103
    entryPointParam_frag_0 = vec4((mix(RU_Linear(RU_Material_0._CoreColor).xyz, RU_Linear(RU_Material_0._SandColor).xyz, vec3(saturate_0(sandData_0.z + density_0 * 0.23999999463558197))) * density_0 + RU_Linear(RU_Material_0._HotColor).xyz * hot_0 * RU_Material_0._GlowIntensity * (0.69999998807907104 + strength_0 * 0.41999998688697815)) * alpha_0 + RU_Linear(RU_Material_0._HotColor).xyz * hot_0 * RU_Material_0._GlowIntensity * alpha_0 * 0.2800000011920929, alpha_0);

#line 103
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
