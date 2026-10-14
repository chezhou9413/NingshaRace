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


#line 103 0
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_0;
};


#line 103
void main()
{

#line 103
    v2f_0 o_0;

#line 103
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 103
    o_0.uv_0 = v_uv_0;

#line 103
    v2f_0 _S1 = o_0;

#line 103
    gl_Position = o_0.vertex_0;

#line 103
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 103
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
