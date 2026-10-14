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
layout(binding = 1, set = 2)
uniform sampler2D _DisplacementTex; // @default = gray
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


#line 3338 1
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 3338
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3338
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 133 0
struct appdata_0
{
    vec4 vertex_0;
    vec2 uv_0;
};


#line 133
struct v2f_0
{
    vec4 vertex_1;
    vec2 uv_1;
};


#line 133
void main()
{

#line 133
    appdata_0 _S1;

#line 133
    _S1.vertex_0 = v_vertex_0;

#line 133
    _S1.uv_0 = v_uv_0;

#line 133
    _S1.vertex_0.xyz = _S1.vertex_0.xyz * 1.79999995231628418;

#line 133
    v2f_0 o_0;

#line 133
    o_0.vertex_1 = UnityObjectToClipPos_0(_S1.vertex_0);

#line 133
    o_0.uv_1 = _S1.uv_0 * 1.79999995231628418 - 0.40000000596046448;

#line 133
    v2f_0 _S2 = o_0;

#line 133
    gl_Position = o_0.vertex_1;

#line 133
    entryPointParam_vert_uv_0 = _S2.uv_1;

#line 133
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
