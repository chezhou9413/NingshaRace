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
    float _Additive;
    float _BendPower;
    float _BladebearerPreviewEnabled;
    float _BladebearerPreviewTime;
    float _Breakup;
    vec4 _BreakupScale;
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DetailStrength;
    vec4 _Displacement;
    float _FlowAngle;
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _NoiseScale;
    float _Phase;
    float _PhaseAge;
    float _PhaseAssemble;
    float _PhaseDissolve;
    float _PhaseDuration;
    float _PhaseInitial;
    float _PhaseLifetime;
    vec4 _Root;
    float _RootLock;
    float _Speed;
    vec4 _Time;
    vec4 _Tip;
};


#line 82
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{    float _BendPower;
    float _BladebearerPreviewEnabled;
    float _BladebearerPreviewTime;
    float _Breakup;
    vec4 _BreakupScale;
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DetailStrength;
    vec4 _Displacement;
    float _FlowAngle;
    vec4 _NoiseScale;
    float _Phase;
    float _PhaseAge;
    float _PhaseAssemble;
    float _PhaseDuration;
    float _PhaseInitial;
    float _PhaseLifetime;
    vec4 _Root;
    float _RootLock;
    float _Speed;
    vec4 _Time;
    vec4 _Tip;
    float _RimUnitySrgb_DisplacementTex; // @default = 1
    float _RimUnitySrgb_MainTex; // @default = 1
}RU_Material_0;

#line 108
layout(binding = 1, set = 2)
uniform sampler2D _DisplacementTex; // @default = gray // @vertex


#line 110



#line 119
layout(binding = 3, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 121



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


#line 112 0
vec4 RU_tex2D_DisplacementTex_0(vec2 uv_0)
{

#line 112
    return (RU_SampleLinear(texture(_DisplacementTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_DisplacementTex));
}


#line 143
vec2 displacedUV_0(vec2 sourceUV_0)
{

#line 143
    if((max(abs(RU_Material_0._Displacement.x), abs(RU_Material_0._Displacement.y))) < 9.99999997475242708e-07)
    {

#line 143
        return sourceUV_0;
    }

#line 143
    vec2 axis_0 = RU_Material_0._Tip.xy - RU_Material_0._Root.xy;

#line 143
    float _S1 = max(dot(axis_0, axis_0), 0.00000999999974738);

#line 143
    float _S2 = (inversesqrt((_S1)));

#line 143
    float along_0 = saturate_0(dot(sourceUV_0 - RU_Material_0._Root.xy, axis_0) / _S1);

#line 143
    float _S3 = radians(RU_Material_0._FlowAngle);

#line 143
    float sinAngle_0 = sin(_S3);

#line 143
    float cosAngle_0 = cos(_S3);

#line 143
    mat2 flowRotation_0 = mat2(cosAngle_0, - sinAngle_0, sinAngle_0, cosAngle_0);

#line 143
    vec2 direction_0 = (((axis_0 * _S2) * (flowRotation_0)));

#line 143
    float time_0 = mix(RU_Material_0._Time.y, RU_Material_0._BladebearerPreviewTime, RU_Material_0._BladebearerPreviewEnabled) * RU_Material_0._Speed + RU_Material_0._Phase;

#line 143
    vec2 relative_0 = sourceUV_0 - RU_Material_0._Root.xy;

#line 143
    float across_0 = dot(relative_0, vec2(- direction_0.y, direction_0.x));

#line 143
    float distance_0 = dot(relative_0, direction_0);

#line 143
    vec2 noiseUV_0 = vec2(RU_Material_0._Phase * 0.37000000476837158 + 0.17299999296665192, distance_0 * RU_Material_0._NoiseScale.y - time_0);

#line 143
    vec2 uv_1 = sourceUV_0 + ((((RU_tex2D_DisplacementTex_0(noiseUV_0).xy * 2.0 - 1.0 + (RU_tex2D_DisplacementTex_0(noiseUV_0 * 2.06999993324279785 + vec2(across_0 * RU_Material_0._NoiseScale.x + 0.31000000238418579, 0.67000001668930054 - time_0 * 0.37000000476837158)).xy * 2.0 - 1.0) * RU_Material_0._DetailStrength) / (1.0 + RU_Material_0._DetailStrength) * RU_Material_0._Displacement.xy) * (flowRotation_0))) * pow(smoothstep(RU_Material_0._RootLock, 1.0, along_0), RU_Material_0._BendPower);

#line 143
    vec2 uv_2;

#line 143
    if((RU_Material_0._Breakup) > 0.00100000004749745)
    {

#line 143
        uv_2 = uv_1 + (((clamp((RU_tex2D_DisplacementTex_0(vec2(across_0 * _S2 * RU_Material_0._BreakupScale.x, distance_0 * _S2 * RU_Material_0._BreakupScale.y - time_0 * 2.20000004768371582) + vec2(RU_Material_0._Phase * 0.43000000715255737, RU_Material_0._Phase * 0.18999999761581421)).xy * 2.0 - 1.0) * 2.5, vec2(-1.0), vec2(1.0)) * RU_Material_0._Displacement.xy) * (flowRotation_0))) * (RU_Material_0._Breakup * smoothstep(RU_Material_0._RootLock + 0.03999999910593033, 0.64999997615814209, along_0) * 0.64999997615814209);

#line 143
    }
    else
    {

#line 143
        uv_2 = uv_1;

#line 143
    }

#line 143
    return uv_2;
}


#line 123
vec4 RU_tex2D_MainTex_0(vec2 uv_3)
{

#line 123
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_3.x, 1.0 - uv_3.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 123
layout(location = 0)
out vec4 entryPointParam_emberFrag_0;


#line 123
layout(location = 1) in vec2 i_source_0; // @vertex TexCoord0 optional


#line 123
layout(location = 5) in vec2 i_uv_0; // @vertex TexCoord1 optional


#line 123
layout(location = 6) in vec2 i_life_0; // @vertex TexCoord2 optional


#line 147
void RU_SourceMain()
{

#line 147
    vec2 sourceUV_1 = displacedUV_0(i_source_0);

#line 147
    float _S4 = sourceUV_1.x;

#line 147
    float _S5 = sourceUV_1.y;

#line 147
    clip_0(min(min(_S4, _S5), min(1.0 - _S4, 1.0 - _S5)));

#line 147
    float _S6 = (RU_tex2D_MainTex_0(sourceUV_1) * RU_Linear(RU_Material_0._Color)).w;

#line 147
    clip_0(_S6 - 0.11999999731779099);

#line 147
    clip_0(i_life_0.x - 0.00100000004749745);

#line 147
    float y_0 = i_uv_0.y;

#line 147
    float x_2 = (i_uv_0.x - 0.5) * 2.0 - sin(y_0 * 4.5) * (1.0 - y_0) * 0.15000000596046448;

#line 147
    float edge_0 = abs(x_2) / max(pow(saturate_0(sin(y_0 * 3.14159274101257324)), 0.69999998807907104) * mix(0.30000001192092896, 1.0, y_0), 0.00100000004749745);

#line 147
    float _S7 = y_0 - 0.64999997615814209;

#line 147
    float alpha_0 = _S6 * i_life_0.x * (smoothstep(0.0, 0.11999999731779099, y_0) * (1.0 - smoothstep(0.85000002384185791, 1.0, y_0))) * saturate_0(1.0 - smoothstep(0.64999997615814209, 1.14999997615814209, edge_0) + (exp2((- edge_0 * edge_0 * 2.70000004768371582))) * 0.15000000596046448);

#line 147
    entryPointParam_emberFrag_0 = vec4(mix(vec3(0.56000000238418579, 0.09000000357627869, 1.0), vec3(1.0, 0.81999999284744263, 1.0), vec3((exp2((- x_2 * x_2 * 22.0 - _S7 * _S7 * 25.0))) * 0.80000001192092896)) * mix(0.80000001192092896, 1.64999997615814209, step(0.72000002861022949, i_life_0.y)) * alpha_0, alpha_0 * 0.72000002861022949);

#line 147
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_emberFrag_0.rgb = RU_Encode(entryPointParam_emberFrag_0.rgb); }

// @rimunity-linear-program 1
