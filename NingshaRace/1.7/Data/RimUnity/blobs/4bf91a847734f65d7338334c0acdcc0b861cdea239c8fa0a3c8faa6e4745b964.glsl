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
uniform sampler2D _GrainTex; // @default = black
layout(binding = 3, set = 2)
uniform sampler2D _NoiseTex; // @default = gray
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _CloudSand; // @default = vec4(0.23, 0.18, 0.115, 1) @color
    vec4 _DeepSand; // @default = vec4(0.13, 0.105, 0.075, 1) @color
    float _FlowTime; // @default = 0
    vec4 _GrainSand; // @default = vec4(0.83, 0.69, 0.46, 1) @color
    float _RimUnitySrgb_GrainTex; // @default = 1
    float _RimUnitySrgb_NoiseTex; // @default = 1
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


#line 91
layout(location = 1)
out vec2 entryPointParam_vert_img_uv_0;


#line 91
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 91
layout(location = 1) in vec2 v_texcoord_0; // @vertex TexCoord0 optional


#line 91
struct v2f_img_0
{
    vec4 pos_0;
    vec2 uv_0;
};


#line 91
void main()
{

#line 91
    v2f_img_0 o_0;

#line 91
    o_0.pos_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 91
    o_0.uv_0 = v_texcoord_0;

#line 91
    v2f_img_0 _S1 = o_0;

#line 91
    gl_Position = o_0.pos_0;

#line 91
    entryPointParam_vert_img_uv_0 = _S1.uv_0;

#line 91
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
