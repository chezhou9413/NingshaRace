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
    float _Alpha; // @default = 0.95
    float _ArcEnd; // @default = 2.85
    float _ArcStart; // @default = -0.18
    vec4 _CrackColor; // @default = vec4(1, 0.83, 0.38, 1) @color
    float _CrackScale; // @default = 28
    float _CrackStrength; // @default = 0.75
    float _CrackWidth; // @default = 0.045
    float _DepthFade; // @default = 0.8
    vec4 _EarthDark; // @default = vec4(0.16, 0.075, 0.025, 1) @color
    vec4 _EarthLight; // @default = vec4(0.95, 0.58, 0.16, 1) @color
    vec4 _EarthMid; // @default = vec4(0.52, 0.24, 0.055, 1) @color
    float _EdgeSoftness; // @default = 0.035
    float _Glow; // @default = 1.4
    vec4 _InnerOffset; // @default = vec4(0.18, -0.1, 0, 0)
    float _InnerRadius; // @default = 0.65
    float _MistAlpha; // @default = 0.28
    vec4 _MistColor; // @default = vec4(0.72, 0.46, 0.18, 1) @color
    float _MistIrregularity; // @default = 0.75
    float _MistScale; // @default = 10
    float _MistSoftness; // @default = 0.12
    float _MistSpread; // @default = 0.16
    float _MistStretch; // @default = 5
    float _MotionSpeed; // @default = 0.35
    float _OuterRadius; // @default = 0.92
    float _RockContrast; // @default = 2.2
    float _RockScale; // @default = 15
    float _Sweep; // @default = 0.16
    vec4 _Time;
    float _TipTaper; // @default = 0.72
    vec4 _WindColor; // @default = vec4(0.86, 0.72, 0.46, 1) @color
    float _WindScale; // @default = 12
    float _WindSharpness; // @default = 3.2
    float _WindSpeed; // @default = 1.2
    float _WindStrength; // @default = 0.65
    float _WindStretch; // @default = 9
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
layout(location = 2)
out vec4 entryPointParam_vert_color_0;


#line 3346
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3346
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 3346
layout(location = 2) in vec4 v_color_0; // @vertex Color optional


#line 131 0
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_0;
    vec4 color_0;
};


#line 131
void main()
{

#line 131
    v2f_0 o_0;

#line 131
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 131
    o_0.uv_0 = v_uv_0;

#line 131
    o_0.color_0 = v_color_0;

#line 131
    v2f_0 _S1 = o_0;

#line 131
    gl_Position = o_0.vertex_0;

#line 131
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 131
    entryPointParam_vert_color_0 = _S1.color_0;

#line 131
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
