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
uniform sampler2D _MaskTex; // @default = black
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AutoFitBox; // @default = 1
    float _BoundaryWarp; // @default = 0.12
    float _BoxPadding; // @default = 0.08
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    vec4 _ColorTwo; // @default = vec4(1, 1, 1, 1) @color
    float _ErosionSoftness; // @default = 0.075
    float _ErosionThreshold; // @default = 0.46
    vec4 _FlowSpeed; // @default = vec4(0.13, 0.28, 0, 0)
    vec4 _FogColor; // @default = vec4(0.005, 0.008, 0.012, 0.92) @color
    float _FogDensity; // @default = 0.9
    float _FogStartY; // @default = 0.5
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    vec4 _MainTex_TexelSize;
    vec4 _MaskTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseDetail; // @default = 0.72
    float _NoiseScale; // @default = 8
    float _PixelAlphaThreshold; // @default = 0.05
    float _PulseStrength; // @default = 0.12
    float _ShapeSoftness; // @default = 0.035
    vec4 _Time;
    float _UseMask; // @default = 0
    float _WarpStrength; // @default = 0.32
    float _RimUnitySrgb_MainTex; // @default = 1
    float _RimUnitySrgb_MaskTex; // @default = 1
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
layout(location = 2)
out vec4 entryPointParam_vert_color_0;


#line 3346
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3346
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 3346
layout(location = 2) in vec4 v_color_0; // @vertex Color optional


#line 127 0
struct v2f_0
{
    vec4 vertex_0;
    vec2 effectUv_0;
    vec4 color_0;
};


#line 127
void main()
{

#line 127
    v2f_0 o_0;

#line 127
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 127
    o_0.effectUv_0 = v_uv_0;

#line 127
    o_0.color_0 = v_color_0;

#line 127
    v2f_0 _S1 = o_0;

#line 127
    gl_Position = o_0.vertex_0;

#line 127
    entryPointParam_vert_effectUv_0 = _S1.effectUv_0;

#line 127
    entryPointParam_vert_color_0 = _S1.color_0;

#line 127
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
