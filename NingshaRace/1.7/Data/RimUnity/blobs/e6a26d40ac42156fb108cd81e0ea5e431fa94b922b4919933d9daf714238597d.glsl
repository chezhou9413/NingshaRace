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
    float _PhaseDissolve;
    vec4 _Root;
    float _RootLock;
    float _Speed;
    vec4 _Time;
    vec4 _Tip;
};


#line 77
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
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _NoiseScale;
    float _Phase;
    float _PhaseDissolve;
    vec4 _Root;
    float _RootLock;
    float _Speed;
    vec4 _Time;
    vec4 _Tip;
    float _RimUnitySrgb_DisplacementTex; // @default = 1
    float _RimUnitySrgb_MainTex; // @default = 1
}RU_Material_0;

#line 98
layout(binding = 1, set = 2)
uniform sampler2D _DisplacementTex; // @default = gray


#line 100



#line 109
layout(binding = 3, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 111



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


#line 102 0
vec4 RU_tex2D_DisplacementTex_0(vec2 uv_0)
{

#line 102
    return (RU_SampleLinear(texture(_DisplacementTex, (vec2(uv_0.x, 1.0 - uv_0.y))), RU_Material_0._RimUnitySrgb_DisplacementTex));
}


#line 133
vec2 displacedUV_0(vec2 sourceUV_0)
{

#line 133
    if((max(abs(RU_Material_0._Displacement.x), abs(RU_Material_0._Displacement.y))) < 9.99999997475242708e-07)
    {

#line 133
        return sourceUV_0;
    }

#line 133
    vec2 axis_0 = RU_Material_0._Tip.xy - RU_Material_0._Root.xy;

#line 133
    float _S1 = max(dot(axis_0, axis_0), 0.00000999999974738);

#line 133
    float _S2 = (inversesqrt((_S1)));

#line 133
    float along_0 = saturate_0(dot(sourceUV_0 - RU_Material_0._Root.xy, axis_0) / _S1);

#line 133
    float _S3 = radians(RU_Material_0._FlowAngle);

#line 133
    float sinAngle_0 = sin(_S3);

#line 133
    float cosAngle_0 = cos(_S3);

#line 133
    mat2 flowRotation_0 = mat2(cosAngle_0, - sinAngle_0, sinAngle_0, cosAngle_0);

#line 133
    vec2 direction_0 = (((axis_0 * _S2) * (flowRotation_0)));

#line 133
    float time_0 = mix(RU_Material_0._Time.y, RU_Material_0._BladebearerPreviewTime, RU_Material_0._BladebearerPreviewEnabled) * RU_Material_0._Speed + RU_Material_0._Phase;

#line 133
    vec2 relative_0 = sourceUV_0 - RU_Material_0._Root.xy;

#line 133
    float across_0 = dot(relative_0, vec2(- direction_0.y, direction_0.x));

#line 133
    float distance_0 = dot(relative_0, direction_0);

#line 133
    vec2 noiseUV_0 = vec2(RU_Material_0._Phase * 0.37000000476837158 + 0.17299999296665192, distance_0 * RU_Material_0._NoiseScale.y - time_0);

#line 133
    vec2 uv_1 = sourceUV_0 + ((((RU_tex2D_DisplacementTex_0(noiseUV_0).xy * 2.0 - 1.0 + (RU_tex2D_DisplacementTex_0(noiseUV_0 * 2.06999993324279785 + vec2(across_0 * RU_Material_0._NoiseScale.x + 0.31000000238418579, 0.67000001668930054 - time_0 * 0.37000000476837158)).xy * 2.0 - 1.0) * RU_Material_0._DetailStrength) / (1.0 + RU_Material_0._DetailStrength) * RU_Material_0._Displacement.xy) * (flowRotation_0))) * pow(smoothstep(RU_Material_0._RootLock, 1.0, along_0), RU_Material_0._BendPower);

#line 133
    vec2 uv_2;

#line 133
    if((RU_Material_0._Breakup) > 0.00100000004749745)
    {

#line 133
        uv_2 = uv_1 + (((clamp((RU_tex2D_DisplacementTex_0(vec2(across_0 * _S2 * RU_Material_0._BreakupScale.x, distance_0 * _S2 * RU_Material_0._BreakupScale.y - time_0 * 2.20000004768371582) + vec2(RU_Material_0._Phase * 0.43000000715255737, RU_Material_0._Phase * 0.18999999761581421)).xy * 2.0 - 1.0) * 2.5, vec2(-1.0), vec2(1.0)) * RU_Material_0._Displacement.xy) * (flowRotation_0))) * (RU_Material_0._Breakup * smoothstep(RU_Material_0._RootLock + 0.03999999910593033, 0.64999997615814209, along_0) * 0.64999997615814209);

#line 133
    }
    else
    {

#line 133
        uv_2 = uv_1;

#line 133
    }

#line 133
    return uv_2;
}


#line 113
vec4 RU_tex2D_MainTex_0(vec2 uv_3)
{

#line 113
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_3.x, 1.0 - uv_3.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 105
vec4 RU_tex2Dlod_DisplacementTex_0(vec4 uv_4)
{

#line 105
    return (RU_SampleLinear(textureLod(_DisplacementTex, (vec2(uv_4.x, 1.0 - uv_4.y)), (uv_4.w)), RU_Material_0._RimUnitySrgb_DisplacementTex));
}


#line 133
float phaseField_0(vec2 uv_5)
{

#line 133
    vec2 noise_0 = RU_tex2Dlod_DisplacementTex_0(vec4(uv_5 * vec2(3.70000004768371582, 4.90000009536743164) + vec2(0.17299999296665192, 0.71899998188018799), 0.0, 0.0)).xy;

#line 133
    return clamp(noise_0.x * 0.47999998927116394 + noise_0.y * 0.23000000417232513 + RU_tex2Dlod_DisplacementTex_0(vec4(uv_5 * 17.29999923706054688 + 0.40999999642372131, 0.0, 0.0)).x * 0.17000000178813934 + saturate_0(uv_5.y) * 0.11999999731779099, 0.01999999955296516, 0.98000001907348633);
}


#line 133
vec4 sampleFlame_0(vec2 sourceUV_1)
{

#line 133
    vec2 uv_6 = displacedUV_0(sourceUV_1);

#line 133
    float _S4 = uv_6.x;

#line 133
    float _S5 = uv_6.y;

#line 133
    float inside_0 = step(0.0, _S4) * step(_S4, 1.0) * step(0.0, _S5) * step(_S5, 1.0);

#line 133
    vec4 c_0 = RU_tex2D_MainTex_0(uv_6 * RU_Material_0._MainTex_ST.zwxy.xy + RU_Material_0._MainTex_ST.zwxy.zw) * RU_Linear(RU_Material_0._Color);

#line 133
    c_0[3] = c_0[3] * inside_0;

#line 133
    if((RU_Material_0._PhaseDissolve) > 0.00000999999974738)
    {

#line 133
        float field_0 = phaseField_0(sourceUV_1);

#line 133
        float mask_0 = smoothstep(RU_Material_0._PhaseDissolve - 0.02199999988079071, RU_Material_0._PhaseDissolve + 0.02199999988079071, field_0);

#line 133
        c_0.xyz = mix(c_0.xyz, c_0.xyz * 0.25 + vec3(0.68000000715255737, 0.2199999988079071, 1.20000004768371582), vec3((1.0 - smoothstep(0.01799999922513962, 0.0949999988079071, field_0 - RU_Material_0._PhaseDissolve)) * saturate_0(RU_Material_0._PhaseDissolve * 18.0)));

#line 133
        c_0[3] = c_0[3] * mask_0;

#line 133
    }

#line 133
    return c_0;
}


#line 133
layout(location = 0)
out vec4 entryPointParam_fragDepth_0;


#line 133
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 133
void RU_SourceMain()
{

#line 133
    clip_0(sampleFlame_0(i_uv_0).w - 0.64999997615814209);

#line 133
    entryPointParam_fragDepth_0 = vec4(ivec4(0));

#line 133
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_fragDepth_0.rgb = RU_Encode(entryPointParam_fragDepth_0.rgb); }

// @rimunity-linear-program 1
