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
    float _AlphaCutoff; // @default = 0.08
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _CrackAmount; // @default = 0.34
    float _CrackScale; // @default = 28
    float _CrackWidth; // @default = 0.022
    float _DetailPreserve; // @default = 0.82
    float _DissolveProgress; // @default = 0
    float _DissolveSoftness; // @default = 0.085
    vec4 _EdgeColor; // @default = vec4(0.58, 0.57, 0.51, 1) @color
    float _EdgeWidth; // @default = 0.035
    float _GrainAmount; // @default = 0.32
    float _GrainScale; // @default = 34
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    float _NoiseContrast; // @default = 1.45
    float _NoiseScale; // @default = 18.2
    float _OriginalTint; // @default = 0.03
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 0.62
    float _RevealProgress; // @default = 1
    float _ShadowInfluence; // @default = 0.86
    vec4 _StoneDark; // @default = vec4(0.16, 0.17, 0.17, 1) @color
    vec4 _StoneLight; // @default = vec4(0.69, 0.68, 0.62, 1) @color
    vec4 _StoneMid; // @default = vec4(0.43, 0.44, 0.42, 1) @color
    float _StoneRoughness; // @default = 0.58
    float _VerticalBias; // @default = 0.4
};


#line 84
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AlphaCutoff; // @default = 0.08
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _CrackAmount; // @default = 0.34
    float _CrackScale; // @default = 28
    float _CrackWidth; // @default = 0.022
    float _DetailPreserve; // @default = 0.82
    float _DissolveProgress; // @default = 0
    float _DissolveSoftness; // @default = 0.085
    vec4 _EdgeColor; // @default = vec4(0.58, 0.57, 0.51, 1) @color
    float _EdgeWidth; // @default = 0.035
    float _GrainAmount; // @default = 0.32
    float _GrainScale; // @default = 34
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    float _NoiseContrast; // @default = 1.45
    float _NoiseScale; // @default = 18.2
    float _OriginalTint; // @default = 0.03
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 0.62
    float _RevealProgress; // @default = 1
    float _ShadowInfluence; // @default = 0.86
    vec4 _StoneDark; // @default = vec4(0.16, 0.17, 0.17, 1) @color
    vec4 _StoneLight; // @default = vec4(0.69, 0.68, 0.62, 1) @color
    vec4 _StoneMid; // @default = vec4(0.43, 0.44, 0.42, 1) @color
    float _StoneRoughness; // @default = 0.58
    float _VerticalBias; // @default = 0.4
    float _RimUnitySrgb_MainTex; // @default = 1
}RU_Material_0;

#line 23
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 3346 1
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 3346
layout(location = 5)
out vec2 entryPointParam_vert_rawUv_0;


#line 3346
layout(location = 2)
out vec4 entryPointParam_vert_color_0;


#line 3346
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3346
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 3346
layout(location = 2) in vec4 v_color_0; // @vertex Color optional


#line 124 0
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_0;
    vec2 rawUv_0;
    vec4 color_0;
};


#line 124
void main()
{

#line 124
    v2f_0 o_0;

#line 124
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 124
    o_0.uv_0 = v_uv_0.xy * RU_Material_0._MainTex_ST.zwxy.xy + RU_Material_0._MainTex_ST.zwxy.zw;

#line 124
    o_0.rawUv_0 = v_uv_0;

#line 124
    o_0.color_0 = v_color_0;

#line 124
    v2f_0 _S1 = o_0;

#line 124
    gl_Position = o_0.vertex_0;

#line 124
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 124
    entryPointParam_vert_rawUv_0 = _S1.rawUv_0;

#line 124
    entryPointParam_vert_color_0 = _S1.color_0;

#line 124
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
