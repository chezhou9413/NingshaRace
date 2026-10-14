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
uniform sampler2D _MainTex; // @default = white


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

#line 23
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 23
layout(location = 1)
out vec4 entryPointParam_vert_uv_0;


#line 23
layout(location = 2)
out vec4 entryPointParam_vert_color_0;


#line 23
layout(location = 5)
out float entryPointParam_vert_age_0;


#line 23
layout(location = 6)
out vec4 entryPointParam_vert_projPos_0;


#line 23
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 23
layout(location = 2) in vec4 v_color_0; // @vertex Color optional


#line 23
layout(location = 1) in vec4 v_texcoord_0; // @vertex TexCoord0 optional


#line 122
struct v2f_0
{
    vec4 vertex_0;
    vec4 uv_0;
    vec4 color_0;
    float age_0;
    vec4 projPos_0;
};


#line 122
void main()
{

#line 122
    v2f_0 o_0;

#line 122
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 122
    o_0.uv_0.xy = v_texcoord_0.xy.xy * RU_Material_0._MainTex_ST.zwxy.xy + RU_Material_0._MainTex_ST.zwxy.zw;

#line 122
    o_0.uv_0.zw = v_texcoord_0.xy;

#line 122
    o_0.color_0 = v_color_0;

#line 122
    o_0.age_0 = 0.5;

#line 122
    o_0.projPos_0 = vec4(ivec4(0));

#line 122
    v2f_0 _S1 = o_0;

#line 122
    gl_Position = o_0.vertex_0;

#line 122
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 122
    entryPointParam_vert_color_0 = _S1.color_0;

#line 122
    entryPointParam_vert_age_0 = _S1.age_0;

#line 122
    entryPointParam_vert_projPos_0 = _S1.projPos_0;

#line 122
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
