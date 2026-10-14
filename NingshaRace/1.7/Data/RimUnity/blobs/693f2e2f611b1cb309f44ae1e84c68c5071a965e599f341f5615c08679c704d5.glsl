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
layout(binding = 1, set = 2)
uniform sampler2D _PositionTex; // @default = white
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AccretionRadius; // @default = 0.8
    float _AccretionWidth; // @default = 0.99
    float _AutoFitBox; // @default = 1
    float _BlackHoleAspect; // @default = 1.7777778
    vec4 _BlackHoleCenter; // @default = vec4(0.5, 0.72, 0, 0)
    float _BlackHoleRadius; // @default = 0.72
    float _BlackHoleSpin; // @default = 1
    float _BlackHoleStrength; // @default = 1
    vec4 _BlackHoleView; // @default = vec4(0, 0, 0, 0)
    float _BoxPadding; // @default = 0.08
    float _DiskBrightness; // @default = 1
    vec4 _DiskColdColor; // @default = vec4(0.5, 0.35, 0.1, 1) @color
    float _DiskFlatten; // @default = 50
    vec4 _DiskHotColor; // @default = vec4(1, 0.8, 0.6, 1) @color
    float _EmissionSoftness; // @default = 0.3
    float _EmissionThreshold; // @default = 0.22
    float _EventHorizon; // @default = 0.3
    float _GlowStrength; // @default = 1
    float _HorizonSoftness; // @default = 0.01
    vec4 _LensColor; // @default = vec4(1, 0.9, 0.7, 1) @color
    float _PixelAlphaThreshold; // @default = 0.05
    vec4 _PositionTex_TexelSize;
    float _RayStepSize; // @default = 0.02
    float _RaymarchSteps; // @default = 200
    vec4 _Time;
    float _ViewportFadeStart; // @default = 0.62
    float _RimUnitySrgb_PositionTex; // @default = 1
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
out vec2 entryPointParam_vert_effectUv_0;


#line 3346
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3346
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 124 0
struct v2f_0
{
    vec4 vertex_0;
    vec2 effectUv_0;
};


#line 124
void main()
{

#line 124
    v2f_0 o_0;

#line 124
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 124
    o_0.effectUv_0 = v_uv_0;

#line 124
    v2f_0 _S1 = o_0;

#line 124
    gl_Position = o_0.vertex_0;

#line 124
    entryPointParam_vert_effectUv_0 = _S1.effectUv_0;

#line 124
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
