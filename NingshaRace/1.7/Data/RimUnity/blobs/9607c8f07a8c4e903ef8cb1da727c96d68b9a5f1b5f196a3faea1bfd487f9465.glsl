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
layout(binding = 3, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 108 0
layout(binding = 1, set = 2)
uniform sampler2D _DisplacementTex; // @default = gray


#line 110



#line 56
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

#line 15
struct RU_CameraData_0
{
    mat4 ViewProj_0;
};


#line 16
layout(binding = 0, std140) uniform block_RU_CameraData_0
{
    mat4 ViewProj_0;
}RU_Camera_0;

#line 17
struct RU_ObjectData_0
{
    mat4 model_0;
};


#line 18
layout(push_constant, std430) uniform block_RU_ObjectData_0
{
    mat4 model_0;
}RU_Object_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 115 0
vec4 RU_tex2Dlod_DisplacementTex_0(vec4 uv_0)
{

#line 115
    return (RU_SampleLinear(textureLod(_DisplacementTex, (vec2(uv_0.x, 1.0 - uv_0.y)), (uv_0.w)), RU_Material_0._RimUnitySrgb_DisplacementTex));
}


#line 143
float phaseField_0(vec2 uv_1)
{

#line 143
    vec2 noise_0 = RU_tex2Dlod_DisplacementTex_0(vec4(uv_1 * vec2(3.70000004768371582, 4.90000009536743164) + vec2(0.17299999296665192, 0.71899998188018799), 0.0, 0.0)).xy;

#line 143
    return clamp(noise_0.x * 0.47999998927116394 + noise_0.y * 0.23000000417232513 + RU_tex2Dlod_DisplacementTex_0(vec4(uv_1 * 17.29999923706054688 + 0.40999999642372131, 0.0, 0.0)).x * 0.17000000178813934 + saturate_0(uv_1.y) * 0.11999999731779099, 0.01999999955296516, 0.98000001907348633);
}


#line 23
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 23
layout(location = 1)
out vec2 entryPointParam_emberVert_source_0;


#line 23
layout(location = 5)
out vec2 entryPointParam_emberVert_uv_0;


#line 23
layout(location = 6)
out vec2 entryPointParam_emberVert_life_0;


#line 23
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 23
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 23
layout(location = 5) in vec2 v_corner_0; // @vertex TexCoord1 optional


#line 147
struct emberOutput_0
{
    vec4 vertex_0;
    vec2 source_0;
    vec2 uv_2;
    vec2 life_0;
};


#line 147
void main()
{

#line 147
    emberOutput_0 o_0;

#line 147
    float field_0 = phaseField_0(v_uv_0);

#line 147
    vec2 seed_0 = RU_tex2Dlod_DisplacementTex_0(vec4(v_uv_0 * 11.89999961853027344 + 0.73000001907348633, 0.0, 0.0)).xy;

#line 147
    float age_0;

#line 147
    if((RU_Material_0._PhaseAssemble) > 0.5)
    {

#line 147
        age_0 = (1.0 - field_0) * RU_Material_0._PhaseDuration - RU_Material_0._PhaseAge;

#line 147
    }
    else
    {

#line 147
        age_0 = RU_Material_0._PhaseAge - saturate_0((field_0 - RU_Material_0._PhaseInitial) / max(1.0 - RU_Material_0._PhaseInitial, 0.00100000004749745)) * RU_Material_0._PhaseDuration;

#line 147
    }

#line 147
    float lifetime_0;

#line 147
    if((RU_Material_0._PhaseAssemble) > 0.5)
    {

#line 147
        lifetime_0 = RU_Material_0._PhaseDuration;

#line 147
    }
    else
    {

#line 147
        lifetime_0 = RU_Material_0._PhaseLifetime * mix(0.64999997615814209, 1.0, seed_0.x);

#line 147
    }

#line 147
    float _S1 = max(age_0, 0.0);

#line 147
    float _S2 = seed_0.x;

#line 147
    float _S3 = seed_0.y;

#line 147
    vec2 direction_0 = normalize(vec2(_S2 - 0.5, 0.18000000715255737 + _S3));

#line 147
    vec2 drift_0 = direction_0 * _S1 * 0.30000001192092896 + (RU_tex2Dlod_DisplacementTex_0(vec4(v_uv_0 * 3.09999990463256836 + vec2(_S1 * 0.28999999165534973, - _S1 * 0.43000000715255737), 0.0, 0.0)).xy - RU_tex2Dlod_DisplacementTex_0(vec4(v_uv_0 * 3.09999990463256836, 0.0, 0.0)).xy) * 0.34999999403953552;

#line 147
    vec2 drift_1;

#line 147
    if((RU_Material_0._PhaseAssemble) > 0.5)
    {

#line 147
        drift_1 = drift_0 + (v_uv_0 - 0.5) * _S1 * 1.10000002384185791;

#line 147
    }
    else
    {

#line 147
        drift_1 = drift_0;

#line 147
    }

#line 147
    vec2 side_0 = vec2(- direction_0.y, direction_0.x);

#line 147
    float size_0 = mix(0.00279999990016222, 0.00680000009015203, _S2 * _S2);

#line 147
    float shrink_0;

#line 147
    if((RU_Material_0._PhaseAssemble) > 0.5)
    {

#line 147
        shrink_0 = 1.0;

#line 147
    }
    else
    {

#line 147
        shrink_0 = mix(1.0, 0.30000001192092896, saturate_0(age_0 / lifetime_0));

#line 147
    }

#line 147
    vec2 corner_0 = (v_corner_0 - 0.5) * 2.0;

#line 147
    vec2 fragmentPosition_0 = v_vertex_0.xz + drift_1 + (side_0 * corner_0.x + direction_0 * corner_0.y * mix(2.09999990463256836, 4.0, _S3)) * size_0 * shrink_0;

#line 147
    o_0.vertex_0 = UnityObjectToClipPos_0(vec4(fragmentPosition_0.x, v_vertex_0.y, fragmentPosition_0.y, 1.0));

#line 147
    o_0.source_0 = v_uv_0;

#line 147
    o_0.uv_2 = v_corner_0;

#line 147
    float _S4 = smoothstep(0.0, 0.05499999970197678, age_0) * (1.0 - smoothstep(lifetime_0 * 0.40000000596046448, lifetime_0, age_0));

#line 147
    float fade_0;

#line 147
    if((RU_Material_0._PhaseAssemble) > 0.5)
    {

#line 147
        fade_0 = smoothstep(0.0, 0.03999999910593033, RU_Material_0._PhaseAge) * smoothstep(0.0, 0.05000000074505806, age_0);

#line 147
    }
    else
    {

#line 147
        fade_0 = _S4;

#line 147
    }

#line 147
    if((RU_Material_0._PhaseAssemble) < 0.5)
    {

#line 147
        fade_0 = fade_0 * step(RU_Material_0._PhaseInitial, field_0);

#line 147
    }
    else
    {

#line 147
    }

#line 147
    o_0.life_0 = vec2(fade_0, _S3);

#line 147
    emberOutput_0 _S5 = o_0;

#line 147
    gl_Position = o_0.vertex_0;

#line 147
    entryPointParam_emberVert_source_0 = _S5.source_0;

#line 147
    entryPointParam_emberVert_uv_0 = _S5.uv_2;

#line 147
    entryPointParam_emberVert_life_0 = _S5.life_0;

#line 147
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
