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


#line 15 0
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
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 3346 1
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 3346
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3346
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 107 0
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_0;
};


#line 107
void main()
{

#line 107
    v2f_0 o_0;

#line 107
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 107
    o_0.uv_0 = v_uv_0;

#line 107
    v2f_0 _S1 = o_0;

#line 107
    gl_Position = o_0.vertex_0;

#line 107
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 107
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
