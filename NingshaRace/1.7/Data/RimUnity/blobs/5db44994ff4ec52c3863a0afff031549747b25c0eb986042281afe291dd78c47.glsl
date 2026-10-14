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


#line 56 0
struct RU_MaterialData_0
{
    float _AlphaCutoff; // @default = 0.092
    float _Brightness; // @default = 0.82
    float _CanvasPadding; // @default = 0.95
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DetailPreserve; // @default = 1
    float _GatherRadius; // @default = 0.991
    float _GrainAmount; // @default = 1
    float _GrainContrast; // @default = 0.2
    float _GrainScale; // @default = 256
    vec4 _MainTex_TexelSize;
    float _OriginalTint; // @default = 0.35
    float _ParticleDensity; // @default = 0.72
    float _ParticleJitter; // @default = 0.6
    float _ParticleScale; // @default = 140
    float _ParticleSize; // @default = 0.62
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 1
    vec4 _SandDark; // @default = vec4(0.358, 0.34, 0.323, 1) @color
    vec4 _SandLight; // @default = vec4(0.66, 0.632, 0.564, 1) @color
    vec4 _SandMid; // @default = vec4(0.679, 0.617, 0.548, 1) @color
    float _SandProgress; // @default = 0.5
    float _ScatterRadius; // @default = 1
    float _ShadowInfluence; // @default = 1
    float _SolidHold; // @default = 0.035
    float _SparkleAmount; // @default = 0.104
};


#line 83
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AlphaCutoff; // @default = 0.092
    float _Brightness; // @default = 0.82
    float _CanvasPadding; // @default = 0.95
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DetailPreserve; // @default = 1
    float _GatherRadius; // @default = 0.991
    float _GrainAmount; // @default = 1
    float _GrainContrast; // @default = 0.2
    float _GrainScale; // @default = 256
    vec4 _MainTex_TexelSize;
    float _OriginalTint; // @default = 0.35
    float _ParticleDensity; // @default = 0.72
    float _ParticleJitter; // @default = 0.6
    float _ParticleScale; // @default = 140
    float _ParticleSize; // @default = 0.62
    vec4 _ReliefLightDirection; // @default = vec4(-0.35, 0.65, 0, 0)
    float _ReliefStrength; // @default = 1
    vec4 _SandDark; // @default = vec4(0.358, 0.34, 0.323, 1) @color
    vec4 _SandLight; // @default = vec4(0.66, 0.632, 0.564, 1) @color
    vec4 _SandMid; // @default = vec4(0.679, 0.617, 0.548, 1) @color
    float _SandProgress; // @default = 0.5
    float _ScatterRadius; // @default = 1
    float _ShadowInfluence; // @default = 1
    float _SolidHold; // @default = 0.035
    float _SparkleAmount; // @default = 0.104
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
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 3346 1
layout(location = 1)
out vec2 entryPointParam_vert_rawUv_0;


#line 3346
layout(location = 5)
out vec2 entryPointParam_vert_meshUv_0;


#line 3346
layout(location = 2)
out vec4 entryPointParam_vert_color_0;


#line 3346
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3346
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 3346
layout(location = 2) in vec4 v_color_0; // @vertex Color optional


#line 140 0
struct appdata_0
{
    vec4 vertex_0;
    vec2 uv_0;
    vec4 color_0;
};


#line 140
struct v2f_0
{
    vec4 vertex_1;
    vec2 rawUv_0;
    vec2 meshUv_0;
    vec4 color_1;
};


#line 140
void main()
{

#line 140
    appdata_0 _S1;

#line 140
    _S1.vertex_0 = v_vertex_0;

#line 140
    _S1.uv_0 = v_uv_0;

#line 140
    _S1.color_0 = v_color_0;

#line 140
    float expansion_0 = 1.0 + 2.0 * max(max(RU_Material_0._GatherRadius, RU_Material_0._ScatterRadius), RU_Material_0._CanvasPadding);

#line 140
    _S1.vertex_0.xz = _S1.vertex_0.xz * expansion_0;

#line 140
    v2f_0 o_0;

#line 140
    o_0.vertex_1 = UnityObjectToClipPos_0(_S1.vertex_0);

#line 140
    o_0.rawUv_0 = (_S1.uv_0 - 0.5) * expansion_0 + 0.5;

#line 140
    o_0.meshUv_0 = _S1.uv_0;

#line 140
    o_0.color_1 = _S1.color_0;

#line 140
    v2f_0 _S2 = o_0;

#line 140
    gl_Position = o_0.vertex_1;

#line 140
    entryPointParam_vert_rawUv_0 = _S2.rawUv_0;

#line 140
    entryPointParam_vert_meshUv_0 = _S2.meshUv_0;

#line 140
    entryPointParam_vert_color_0 = _S2.color_1;

#line 140
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
